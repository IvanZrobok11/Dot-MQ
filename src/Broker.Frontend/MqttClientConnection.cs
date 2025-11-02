using Broker.Core;
using Broker.Core.Routing;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.Sockets;

namespace Broker.Frontend;

public sealed class MqttClientConnection(
    TcpClient tcp,
    PacketDispatcher dispatcher,
    IMqttTcpServer server,
    ILogger<MqttClientConnection> log) : IMqttConnection, IDisposable
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly SemaphoreSlim _disconnectLock = new(1, 1);
    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

    private string _clientId = string.Empty;
    private int _isConnected = 1;
    private long _lastActivity = DateTime.UtcNow.Ticks;

    public string ClientId => _clientId;
    public bool IsConnected => Interlocked.CompareExchange(ref _isConnected, 0, 0) == 1 && tcp.Connected;
    public DateTime LastActivity => new(Interlocked.Read(ref _lastActivity));

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        byte[]? buffer = null;

        try
        {
            buffer = _pool.Rent(8192);
            var stream = tcp.GetStream();
            var packetBuffer = new ArrayBufferWriter<byte>(8192);

            await ReadLoopAsync(stream, packetBuffer, buffer, cancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Fatal error in ProcessAsync");
        }
        finally
        {
            if (buffer != null) _pool.Return(buffer);
            await DisconnectInternalAsync();
        }
    }

    private async Task ReadLoopAsync(
        NetworkStream stream,
        ArrayBufferWriter<byte> packetBuffer,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && IsConnected)
        {
            var bytesRead = await ReadFromStreamAsync(stream, buffer, cancellationToken);
            if (bytesRead <= 0) break;

            packetBuffer.Write(buffer.AsSpan(0, bytesRead));
            UpdateLastActivity();

            await HandleReceivedBytesAsync(packetBuffer, cancellationToken);
        }
    }

    private async Task<int> ReadFromStreamAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        try
        {
            return await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            log.LogDebug("Read canceled");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Stream read error");
        }
        return 0;
    }

    private async Task HandleReceivedBytesAsync(ArrayBufferWriter<byte> packetBuffer, CancellationToken cancellationToken)
    {
        while (TryParsePacket(packetBuffer, out var packet))
        {
            if (packet == null) return;

            // CONNECT must be first
            if (string.IsNullOrEmpty(_clientId) && packet.PacketType != MqttPacketType.CONNECT)
            {
                log.LogWarning("Received {Type} before CONNECT", packet.PacketType);
                RequestDisconnect("CONNECT required");
                return;
            }
            else
            {
                if (string.IsNullOrEmpty(_clientId))
                {
                    _clientId = Guid.NewGuid().ToString("N");
                }
            }

            try
            {
                await dispatcher.DispatchAsync(this, packet, cancellationToken);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error dispatching {Type} for {Client}", packet.PacketType, _clientId);
                RequestDisconnect("Dispatch error");
            }
        }
    }

    private bool TryParsePacket(ArrayBufferWriter<byte> buffer, out MqttPacket? packet)
    {
        packet = null;
        var span = buffer.WrittenSpan;

        if (!TryParsePacketHeader(span, out var remainingLength, out var headerLength))
            return false;

        var totalSize = headerLength + remainingLength;
        if (span.Length < totalSize)
            return false;

        if (!TryParsePacketBody(span.Slice(0, totalSize), out packet))
        {
            RequestDisconnect("Parse error");
            return false;
        }

        AdvanceBuffer(buffer, totalSize);
        return true;
    }

    private bool TryParsePacketHeader(ReadOnlySpan<byte> data, out int remainingLength, out int headerLength)
    {
        remainingLength = 0;
        headerLength = 1;

        if (data.Length < 2) return false;

        int multiplier = 1;

        for (int i = 1; i < data.Length && i < 5; i++)
        {
            byte b = data[i];
            remainingLength += (b & 0x7F) * multiplier;
            multiplier *= 128;
            headerLength++;

            if ((b & 0x80) == 0)
                return true;
        }

        return false;
    }

    private bool TryParsePacketBody(ReadOnlySpan<byte> packetBytes, out MqttPacket? packet)
    {
        packet = null;

        try
        {
            var arr = packetBytes.ToArray();
            packet = MqttPacketParser.Parse(arr);
            return true;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Packet parse failed");
            return false;
        }
    }

    private void AdvanceBuffer(ArrayBufferWriter<byte> buffer, int count)
    {
        var written = buffer.WrittenSpan;
        var remaining = written.Slice(count);

        buffer.Clear();
        if (remaining.Length > 0)
            buffer.Write(remaining);
    }

    public async Task SendAsync(MqttPacket packet, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Client not connected");

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            if (!IsConnected) return;

            var stream = tcp.GetStream();
            var bytes = packet.Serialize();

            await stream.WriteAsync(bytes.AsMemory(0, bytes.Length), cancellationToken);
            await stream.FlushAsync(cancellationToken);

            UpdateLastActivity();
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public void RequestDisconnect(string reason)
    {
        log.LogDebug("Request disconnect: {Reason}", reason);

        try { tcp.Client.Shutdown(SocketShutdown.Both); }
        catch { }
    }

    private void UpdateLastActivity()
    {
        Interlocked.Exchange(ref _lastActivity, DateTime.UtcNow.Ticks);
    }

    private async Task DisconnectInternalAsync()
    {
        if (Interlocked.CompareExchange(ref _isConnected, 0, 1) == 0)
            return;

        await _disconnectLock.WaitAsync();
        try
        {
            SafeShutdownSocket();
            await server.UnregisterClientConnectionAsync(_clientId);
            SafeCloseTcpClient();
        }
        finally
        {
            _disconnectLock.Release();
        }
    }

    private void SafeShutdownSocket()
    {
        try { tcp.Client?.Shutdown(SocketShutdown.Both); }
        catch { }
    }

    private void SafeCloseTcpClient()
    {
        try { tcp.Close(); }
        catch { }
    }

    public void Dispose()
    {
        try { tcp.Client?.Shutdown(SocketShutdown.Both); } catch { }
        try { tcp.Dispose(); } catch { }

        _sendLock.Dispose();
        _disconnectLock.Dispose();
    }
}
