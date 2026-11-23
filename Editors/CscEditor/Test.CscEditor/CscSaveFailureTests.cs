using System.Reflection;
using System.Runtime.CompilerServices;
using Editors.CscEditor.Data;
using Editors.CscEditor.Services;
using Editors.CscEditor.ViewModels;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;

namespace Test.CscEditor
{
    /// <summary>The editor's file transaction (load, dirty state, save), exercised without a GPU
    /// scene: the view model is created uninitialised and only the services the load/save path
    /// touches are filled in.</summary>
    public class CscSaveFailureTests
    {
        sealed class Setup
        {
            public required CscEditorViewModel Editor { get; init; }
            public required CscScene Scene { get; init; }
            public required PackFile OriginalFile { get; init; }
            public int ExceptionWindows;
            public string? SavedPath;
            public byte[]? SavedBytes;
            public bool SaveSucceeds = true;
        }

        static Setup Create()
        {
            var scene = CscScene.Load(CscSceneRoundTripTests.BuildSceneWithManifestBytes());
            var originalBytes = CscSceneWriter.Write(scene);
            var originalFile = PackFile.CreateFromBytes("original.csc", originalBytes);
            var editor = (CscEditorViewModel)RuntimeHelpers.GetUninitializedObject(typeof(CscEditorViewModel));
            var setup = new Setup { Editor = editor, Scene = scene, OriginalFile = originalFile };

            SetField(editor, "_logger", Serilog.Core.Logger.None);
            SetField(editor, "_dialogs", Proxy<IStandardDialogs>((method, _) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IStandardDialogs.ShowExceptionWindow)));
                setup.ExceptionWindows++;
                return null;
            }));
            SetField(editor, "_packFileService", Proxy<IPackFileService>((method, arguments) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IPackFileService.GetFullPath)));
                return @"composite_scene\" + ((PackFile)arguments![0]!).Name;
            }));
            SetField(editor, "_fileSaveService", Proxy<IFileSaveService>((method, arguments) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IFileSaveService.Save)));
                setup.SavedPath = (string)arguments![0]!;
                setup.SavedBytes = (byte[])arguments[1]!;
                return setup.SaveSucceeds ? PackFile.CreateFromBytes("original.csc", setup.SavedBytes) : null;
            }));

            var history = new CscEditHistory(() => CscSceneWriter.Write(scene), () => CscSelectionSnapshot.None, (_, _) => { });
            history.InitializeLoadedDocument(originalBytes);
            SetField(editor, "_editHistory", history);
            SetProperty(editor, nameof(CscEditorViewModel.SceneData), scene);
            SetProperty(editor, nameof(CscEditorViewModel.CurrentFile), originalFile);
            editor.DisplayName = originalFile.Name;

            scene.Duration += 2;
            history.RecordChange();
            return setup;
        }

        [Test]
        public void Failed_replacement_load_keeps_previous_document_and_save_destination()
        {
            var setup = Create();
            var editor = setup.Editor;

            editor.LoadFile(PackFile.CreateFromBytes("invalid.csc", [0, 1, 2, 3]));

            Assert.Multiple(() =>
            {
                Assert.That(setup.ExceptionWindows, Is.EqualTo(1));
                Assert.That(editor.CurrentFile, Is.SameAs(setup.OriginalFile));
                Assert.That(editor.DisplayName, Is.EqualTo("original.csc"));
                Assert.That(editor.SceneData, Is.SameAs(setup.Scene));
                Assert.That(editor.HasUnsavedChanges, Is.True);
            });

            Assert.That(editor.Save(), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(setup.SavedPath, Is.EqualTo(@"composite_scene\original.csc"));
                Assert.That(CscScene.Load(setup.SavedBytes!).Duration, Is.EqualTo(setup.Scene.Duration));
                Assert.That(editor.HasUnsavedChanges, Is.False);
            });
        }

        [Test]
        public void A_save_that_does_not_complete_keeps_the_unsaved_changes()
        {
            var setup = Create();
            setup.SaveSucceeds = false;

            Assert.That(setup.Editor.Save(), Is.False);
            Assert.That(setup.Editor.HasUnsavedChanges, Is.True);
        }

        static void SetField(object target, string name, object value) =>
            typeof(CscEditorViewModel).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

        static void SetProperty(object target, string name, object value) =>
            typeof(CscEditorViewModel).GetProperty(name)!.SetValue(target, value);

        static T Proxy<T>(Func<MethodInfo, object?[]?, object?> callback) where T : class
        {
            var proxy = DispatchProxy.Create<T, TestProxy>();
            ((TestProxy)(object)proxy).Callback = callback;
            return proxy;
        }

        public class TestProxy : DispatchProxy
        {
            public Func<MethodInfo, object?[]?, object?> Callback { get; set; } = null!;
            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Callback(targetMethod!, args);
        }
    }
}
