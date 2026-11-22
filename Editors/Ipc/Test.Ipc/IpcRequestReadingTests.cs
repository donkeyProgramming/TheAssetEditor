using System.IO.Pipes;
using Editors.Ipc;

namespace Test.Ipc
{
    // Ported from Szy-Cathay/TheAssetEditor-CN (bounded IPC reads)
    public class IpcRequestReadingTests
    {
        static Task<string?> Read(string text, int maxChars = 100) =>
            AssetEditorIpcServer.ReadBoundedLineAsync(new StringReader(text), maxChars, CancellationToken.None);

        [TestCase("{\"action\":\"open\"}\n", "{\"action\":\"open\"}")]
        [TestCase("{\"a\":1}\r\n", "{\"a\":1}")]
        [TestCase("{\"a\":1}\nignored", "{\"a\":1}")]
        [TestCase("no newline at the end", "no newline at the end")]
        [TestCase("keeps a lone \r inside", "keeps a lone \r inside")]
        public async Task ReadsOneLine(string sent, string expected)
        {
            Assert.That(await Read(sent), Is.EqualTo(expected));
        }

        [Test]
        public async Task NothingSent_IsNull()
        {
            Assert.That(await Read(""), Is.Null);
        }

        [Test]
        public async Task LineAtTheLimit_IsAccepted()
        {
            Assert.That(await Read(new string('x', 100) + "\n"), Has.Length.EqualTo(100));
        }

        [Test]
        public void LineOverTheLimit_IsRejected()
        {
            Assert.ThrowsAsync<InvalidDataException>(() => Read(new string('x', 101) + "\n"));
        }

        [Test]
        public void ClientThatSendsNothing_TimesOut()
        {
            // A real pipe whose client connects and then stays silent
            var name = "AssetEditorIpcTest_" + Guid.NewGuid().ToString("N");
            using var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
            var accept = server.WaitForConnectionAsync();
            client.Connect(5000);
            accept.Wait(5000);

            using var reader = new StreamReader(server, leaveOpen: true);
            using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            Assert.That(async () => await AssetEditorIpcServer.ReadBoundedLineAsync(reader, 100, timeout.Token),
                Throws.InstanceOf<OperationCanceledException>());
        }
    }
}
