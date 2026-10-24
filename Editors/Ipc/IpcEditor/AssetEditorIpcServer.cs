using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Editors.Ipc
{
    public class AssetEditorIpcServer : IDisposable
    {
        public const string PipeName = "TheAssetEditor.Ipc";
        public const string OwnershipMutexName = @"Local\TheAssetEditor.Ipc.Owner";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ILogger _logger = Logging.Create<AssetEditorIpcServer>();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly object _syncLock = new();

        private CancellationTokenSource? _cancellationTokenSource;
        private Thread? _serverThread;
        private NamedPipeServerStream? _activePipe;
        private bool _disposed;

        public AssetEditorIpcServer(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public void Start()
        {
            lock (_syncLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(AssetEditorIpcServer));

                if (_serverThread != null)
                    return;

                var cancellationTokenSource = new CancellationTokenSource();
                var serverThread = new Thread(() => RunOwnershipLoop(cancellationTokenSource.Token))
                {
                    IsBackground = true,
                    Name = "AssetEditor IPC ownership"
                };

                _cancellationTokenSource = cancellationTokenSource;
                _serverThread = serverThread;

                try
                {
                    serverThread.Start();
                }
                catch
                {
                    _cancellationTokenSource = null;
                    _serverThread = null;
                    cancellationTokenSource.Dispose();
                    throw;
                }
            }
        }

        private void RunOwnershipLoop(CancellationToken cancellationToken)
        {
            try
            {
                using var ownershipMutex = new Mutex(false, OwnershipMutexName);
                _logger.Here().Information($"Waiting for IPC ownership on {OwnershipMutexName}");

                var ownsMutex = false;
                try
                {
                    int waitResult;
                    try
                    {
                        waitResult = WaitHandle.WaitAny([ownershipMutex, cancellationToken.WaitHandle]);
                    }
                    catch (AbandonedMutexException ex) when (ex.MutexIndex == 0)
                    {
                        // The previous owner exited without releasing the mutex. Windows
                        // grants ownership to this thread, so it is safe to start the pipe.
                        waitResult = 0;
                        _logger.Here().Warning("Taking over IPC ownership from an exited Asset Editor instance");
                    }

                    if (waitResult != 0)
                        return;

                    ownsMutex = true;
                    _logger.Here().Information("Acquired IPC ownership");

                    // A Windows mutex must be released by the thread that acquired it.
                    // Keep this thread alive while the async server loop runs elsewhere.
                    RunServerLoopAsync(cancellationToken).GetAwaiter().GetResult();
                }
                finally
                {
                    if (ownsMutex)
                    {
                        ownershipMutex.ReleaseMutex();
                        _logger.Here().Information("Released IPC ownership");
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "IPC ownership coordinator stopped unexpectedly");
            }
        }

        private async Task RunServerLoopAsync(CancellationToken cancellationToken)
        {
            _logger.Here().Information($"Starting IPC named pipe server on {PipeName}");

            while (cancellationToken.IsCancellationRequested == false)
            {
                NamedPipeServerStream? pipe = null;
                var retryAfterFailure = false;
                try
                {
                    pipe = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                    SetActivePipe(pipe);

                    await pipe.WaitForConnectionAsync(cancellationToken);

                    var response = await ProcessRequestAsync(pipe, cancellationToken);
                    await WriteResponseAsync(pipe, response);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Here().Error(ex, "Unhandled exception in IPC server loop");
                    retryAfterFailure = true;
                }
                finally
                {
                    if (pipe != null)
                        ClearActivePipe(pipe);

                    pipe?.Dispose();
                }

                if (retryAfterFailure)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }

            _logger.Here().Information("IPC named pipe server stopped");
        }

        private async Task<IpcResponse> ProcessRequestAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(pipe, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line))
                return IpcResponse.Failure("Empty request");

            IpcRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<IpcRequest>(line, SerializerOptions);
            }
            catch (JsonException)
            {
                return IpcResponse.Failure("Invalid JSON");
            }

            if (request == null)
                return IpcResponse.Failure("Invalid JSON");

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IIpcRequestHandler>();

            try
            {
                return await handler.HandleAsync(request, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return IpcResponse.Failure("Canceled");
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "IPC request handling failed");
                return IpcResponse.Failure("Internal server error");
            }
        }

        private static async Task WriteResponseAsync(NamedPipeServerStream pipe, IpcResponse response)
        {
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), bufferSize: 1024, leaveOpen: true)
            {
                AutoFlush = true
            };

            var json = JsonSerializer.Serialize(response, SerializerOptions);
            await writer.WriteLineAsync(json);
        }

        private void SetActivePipe(NamedPipeServerStream pipe)
        {
            lock (_syncLock)
            {
                _activePipe = pipe;
            }
        }

        private void ClearActivePipe(NamedPipeServerStream pipe)
        {
            lock (_syncLock)
            {
                if (ReferenceEquals(_activePipe, pipe))
                    _activePipe = null;
            }
        }

        public void Dispose()
        {
            CancellationTokenSource? cancellationTokenSource;
            Thread? serverThread;
            NamedPipeServerStream? activePipe;

            lock (_syncLock)
            {
                if (_disposed)
                    return;

                _disposed = true;
                cancellationTokenSource = _cancellationTokenSource;
                serverThread = _serverThread;
                activePipe = _activePipe;

                _cancellationTokenSource = null;
                _serverThread = null;
                _activePipe = null;
            }

            try
            {
                cancellationTokenSource?.Cancel();
            }
            catch
            {
            }

            try
            {
                activePipe?.Dispose();
            }
            catch
            {
            }

            var serverStopped = true;
            if (serverThread != null)
            {
                try
                {
                    serverStopped = serverThread.Join(TimeSpan.FromSeconds(2));
                }
                catch
                {
                    serverStopped = false;
                }
            }

            if (serverStopped)
                cancellationTokenSource?.Dispose();
        }
    }
}
