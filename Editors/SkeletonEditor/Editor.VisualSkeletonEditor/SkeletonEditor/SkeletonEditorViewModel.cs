using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Animation;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editor.VisualSkeletonEditor.SkeletonEditor
{
    public struct TransformUndoState
    {
        public int BoneIndex;
        public Vector3 Translation;
        public Vector3 Rotation;
    }

    public partial class SkeletonEditorViewModel : EditorHostBase, IFileEditor
    {
        SceneObject _techSkeletonNode;

        private readonly IPackFileService _packFileService;
        private readonly CopyPasteManager _copyPasteManager;
        private readonly IStandardDialogs _packFileUiProvider;
        private readonly IFileSaveService _packFileSaveService;

        [ObservableProperty] string _skeletonName = "";
        [ObservableProperty] string _refMeshName = "";
        [ObservableProperty] string _sourceSkeletonName = "";
        [ObservableProperty] bool _showBonesAsWorldTransform = true;
        [ObservableProperty] ObservableCollection<SkeletonBoneNode> _bones = new();
        [ObservableProperty] SkeletonBoneNode? _selectedBone = null;
        [ObservableProperty] bool _isTechSkeleton = false;
        [ObservableProperty] float _boneVisualScale = 1.5f;
        [ObservableProperty] float _boneScale = 1;
        [ObservableProperty] Vector3ViewModel _selectedBoneRotationOffset;
        [ObservableProperty] Vector3ViewModel _selectedBoneTranslationOffset;
        [ObservableProperty] string _selectedBoneName;
        [ObservableProperty] bool _showSkeleton = true;
        [ObservableProperty] bool _showRefMesh = true;

        public override Type EditorViewModelType => typeof(EditorView);

        // --- Win32 APIs ---
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        static extern int ShowCursor(bool bShow);

        // --- 新增：用于判断当前软件是否处于系统的最前端 ---
        [DllImport("user32.dll")]
        static extern System.IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(System.IntPtr hWnd, out uint lpdwProcessId);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point { public int X; public int Y; }

        private string _interactMode = "None";
        private string _lockAxis = "None";
        private bool _isCursorHidden = false;

        private System.Windows.Point _initialMousePos;
        private double _virtualDeltaX = 0;
        private double _virtualDeltaY = 0;

        private Vector3 _originalTrans;
        private Vector3 _originalRot;

        private Vector3 _camRight = Vector3.Right;
        private Vector3 _camUp = Vector3.Up;

        private Stack<TransformUndoState> _undoStack = new Stack<TransformUndoState>();
        private bool _wasZPressed = false;

        // 保存当前软件的进程 ID
        private readonly uint _currentProcessId;

        public SkeletonEditorViewModel(
            IPackFileService pfs,
            CopyPasteManager copyPasteManager,
            IEditorHostParameters editorHostParameters,
            IStandardDialogs packFileUiProvider,
            IFileSaveService packFileSaveService)
            : base(editorHostParameters)
        {
            DisplayName = "Skeleton Editor";

            _packFileService = pfs;
            _copyPasteManager = copyPasteManager;
            _packFileUiProvider = packFileUiProvider;
            _packFileSaveService = packFileSaveService;
            _selectedBoneRotationOffset = new Vector3ViewModel(0, 0, 0, x => HandleTranslationChanged());
            _selectedBoneTranslationOffset = new Vector3ViewModel(0, 0, 0, x => HandleTranslationChanged());

            // 缓存当前进程的 ID，用于性能优化
            _currentProcessId = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;

            System.Windows.Media.CompositionTarget.Rendering += OnInteractionTick;

            Initialize();
        }

        public override void Close()
        {
            RestoreCursor();
            System.Windows.Media.CompositionTarget.Rendering -= OnInteractionTick;
            base.Close();
        }

        private void HideCursor()
        {
            if (!_isCursorHidden) { ShowCursor(false); _isCursorHidden = true; }
        }

        private void RestoreCursor()
        {
            if (_isCursorHidden) { ShowCursor(true); _isCursorHidden = false; }
        }

        private Matrix GetCameraViewMatrix()
        {
            try
            {
                var cameraProp = FocusService.GetType().GetProperty("Camera");
                if (cameraProp != null)
                {
                    var cameraObj = cameraProp.GetValue(FocusService);
                    if (cameraObj != null)
                    {
                        var viewProp = cameraObj.GetType().GetProperty("ViewMatrix") ?? cameraObj.GetType().GetProperty("View");
                        if (viewProp != null)
                        {
                            return (Matrix)viewProp.GetValue(cameraObj);
                        }
                    }
                }
            }
            catch { }
            return Matrix.Identity;
        }

        public void UndoAction()
        {
            if (_undoStack.Count > 0)
            {
                var state = _undoStack.Pop();
                if (SelectedBone == null || SelectedBone.BoneIndex != state.BoneIndex)
                {
                    var targetBone = Bones.FirstOrDefault(b => b.BoneIndex == state.BoneIndex);
                    if (targetBone != null) SelectedBone = targetBone;
                }
                if (SelectedBone != null)
                {
                    RestoreCursor();
                    _interactMode = "None";
                    SelectedBoneTranslationOffset.Set(state.Translation);
                    SelectedBoneRotationOffset.Set(state.Rotation);
                }
            }
        }

        private void OnInteractionTick(object? sender, EventArgs e)
        {
            // ---------------------------------------------------------
            // 核心安全验证：检查系统当前激活的窗口是否属于我们这个软件
            // ---------------------------------------------------------
            System.IntPtr fgWindow = GetForegroundWindow();
            GetWindowThreadProcessId(fgWindow, out uint activeProcId);

            if (activeProcId != _currentProcessId)
            {
                // 如果用户切到了其他软件（Alt+Tab），且当前正处于拖拽模式，立刻安全取消并恢复鼠标！
                if (_interactMode != "None")
                {
                    if (_interactMode == "Translate") SelectedBoneTranslationOffset.Set(_originalTrans);
                    if (_interactMode == "Rotate") SelectedBoneRotationOffset.Set(_originalRot);
                    _interactMode = "None";
                    RestoreCursor();
                }
                return; // 屏蔽后续所有的快捷键读取
            }

            // Block hotkeys if user is typing in a TextBox
            if (System.Windows.Input.Keyboard.FocusedElement is System.Windows.Controls.TextBox)
                return;

            bool ctrlPressed = (GetAsyncKeyState(0x11) & 0x8000) != 0;
            bool shiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0;

            bool zPressed = (GetAsyncKeyState(0x5A) & 0x8000) != 0;
            bool xPressed = (GetAsyncKeyState(0x58) & 0x8000) != 0;
            bool yPressed = (GetAsyncKeyState(0x59) & 0x8000) != 0;

            bool gPressed = (GetAsyncKeyState(0x47) & 0x8000) != 0;
            bool rPressed = (GetAsyncKeyState(0x52) & 0x8000) != 0;
            bool escPressed = (GetAsyncKeyState(0x1B) & 0x8000) != 0;
            bool lmbPressed = (GetAsyncKeyState(0x01) & 0x8000) != 0;
            bool rmbPressed = (GetAsyncKeyState(0x02) & 0x8000) != 0;

            if (ctrlPressed && zPressed)
            {
                if (!_wasZPressed) { UndoAction(); _wasZPressed = true; }
            }
            else
            {
                _wasZPressed = false;
            }

            if (_interactMode == "None")
            {
                if (SelectedBone == null) return;

                if ((gPressed || rPressed) && !ctrlPressed)
                {
                    _interactMode = gPressed ? "Translate" : "Rotate";
                    _lockAxis = "None";
                    _originalTrans = SelectedBoneTranslationOffset.GetAsVector3();
                    _originalRot = SelectedBoneRotationOffset.GetAsVector3();

                    Matrix viewMatrix = GetCameraViewMatrix();
                    Matrix invView = Matrix.Invert(viewMatrix);

                    if (!float.IsNaN(invView.M11))
                    {
                        _camRight = invView.Right;
                        _camUp = invView.Up;
                    }

                    Win32Point pt = new Win32Point();
                    GetCursorPos(ref pt);
                    _initialMousePos = new System.Windows.Point(pt.X, pt.Y);
                    _virtualDeltaX = 0;
                    _virtualDeltaY = 0;

                    HideCursor();
                }
            }
            else
            {
                if (lmbPressed)
                {
                    _undoStack.Push(new TransformUndoState { BoneIndex = SelectedBone.BoneIndex, Translation = _originalTrans, Rotation = _originalRot });
                    _interactMode = "None";
                    RestoreCursor();
                    return;
                }
                if (rmbPressed || escPressed)
                {
                    if (_interactMode == "Translate") SelectedBoneTranslationOffset.Set(_originalTrans);
                    if (_interactMode == "Rotate") SelectedBoneRotationOffset.Set(_originalRot);
                    _interactMode = "None";
                    RestoreCursor();
                    return;
                }

                if (xPressed) _lockAxis = "X";
                if (yPressed) _lockAxis = "Y";
                if (zPressed && !ctrlPressed) _lockAxis = "Z";

                Win32Point pt = new Win32Point();
                GetCursorPos(ref pt);
                double frameDeltaX = pt.X - _initialMousePos.X;
                double frameDeltaY = pt.Y - _initialMousePos.Y;

                if (frameDeltaX != 0 || frameDeltaY != 0)
                {
                    double accelerationMult = shiftPressed ? 0.1 : 1.0;
                    _virtualDeltaX += frameDeltaX * accelerationMult;
                    _virtualDeltaY += frameDeltaY * accelerationMult;

                    SetCursorPos((int)_initialMousePos.X, (int)_initialMousePos.Y);
                }

                if (_interactMode == "Translate")
                {
                    float baseSens = 0.005f;
                    var trans = _originalTrans;

                    if (_lockAxis == "None")
                    {
                        trans -= _camRight * (float)_virtualDeltaX * baseSens;
                        trans -= _camUp * (float)_virtualDeltaY * baseSens;
                    }
                    else if (_lockAxis == "X") trans.X -= (float)_virtualDeltaX * baseSens;
                    else if (_lockAxis == "Y") trans.Y -= (float)_virtualDeltaY * baseSens;
                    else if (_lockAxis == "Z") trans.Z -= (float)_virtualDeltaY * baseSens;

                    SelectedBoneTranslationOffset.Set(trans);
                }
                else if (_interactMode == "Rotate")
                {
                    float baseSens = 0.5f;
                    var rot = _originalRot;

                    if (_lockAxis == "None")
                    {
                        rot.Y += (float)_virtualDeltaX * baseSens;
                        rot.X -= (float)_virtualDeltaY * baseSens;
                    }
                    else if (_lockAxis == "X") rot.X -= (float)_virtualDeltaY * baseSens;
                    else if (_lockAxis == "Y") rot.Y += (float)_virtualDeltaX * baseSens;
                    else if (_lockAxis == "Z") rot.Z += (float)_virtualDeltaX * baseSens;

                    SelectedBoneRotationOffset.Set(rot);
                }
            }
        }

        void Initialize()
        {
            var assetNode = _sceneObjectViewModelBuilder.CreateAsset("Skeleton", false, "SkeletonNode", Color.Black, null);
            assetNode.IsControlVisible = false;
            _techSkeletonNode = assetNode.Data;

            SceneObjects.Add(assetNode);
        }

        partial void OnShowBonesAsWorldTransformChanged(bool value) => RefreshBoneInformation(SelectedBone);
        partial void OnSelectedBoneChanged(SkeletonBoneNode? value) => RefreshBoneInformation(value);
        partial void OnIsTechSkeletonChanged(bool value) => SetTechSkeletonTransform(value);
        partial void OnBoneVisualScaleChanged(float value) => _techSkeletonNode?.SelectedBoneScale(value);
        partial void OnBoneScaleChanged(float value) => BoneTransformHandler.Scale(SelectedBone, _techSkeletonNode.Skeleton, (float)BoneScale);
        partial void OnSelectedBoneNameChanged(string value) => UpdateSelectedBoneName(value);
        partial void OnShowSkeletonChanged(bool value) => _techSkeletonNode.ShowSkeleton.Value = value;
        partial void OnShowRefMeshChanged(bool value) => _techSkeletonNode.ShowMesh.Value = value;

        public PackFile CurrentFile { get; private set; }
        public void LoadFile(PackFile file)
        {
            CurrentFile = file;
            var skeletonPath = _packFileService.GetFullPath(file);
            LoadSkeleton(_techSkeletonNode, skeletonPath);
        }

        void LoadSkeleton(SceneObject techSkeletonNode, string skeletonPath)
        {
            try
            {
                DisplayName = Path.GetFileName(skeletonPath);
                _techSkeletonNode = techSkeletonNode;

                RefreshBoneInformation(null);
                var packFile = _packFileService.FindFile(skeletonPath);
                SkeletonName = skeletonPath;
                SceneObjectEditor.SetSkeleton(_techSkeletonNode, packFile);
                RefreshBoneList();
                IsTechSkeleton = skeletonPath.ToLower().Contains("tech");
                SourceSkeletonName = _techSkeletonNode.Skeleton.SkeletonName;

            }
            catch (Exception e)
            {
                MessageBox.Show($"Unable to load skeleton '{skeletonPath}'\n\n" + e.Message);
            }
        }

        void RefreshBoneList(int boneToSelect = -1)
        {
            Bones.Clear();
            if (_techSkeletonNode?.Skeleton == null)
                return;

            var bones = SkeletonBoneNodeHelper.CreateBoneOverview(_techSkeletonNode.Skeleton);
            foreach (var bone in bones)
                Bones.Add(bone);

            if (boneToSelect >= 0 && boneToSelect < _techSkeletonNode.Skeleton.BoneCount)
                SelectedBone = Bones[boneToSelect];
        }

        void RefreshBoneInformation(SkeletonBoneNode selectedBone)
        {
            SelectedBoneRotationOffset.DisableCallbacks = true;
            SelectedBoneTranslationOffset.DisableCallbacks = true;

            if (selectedBone == null)
            {
                SelectedBoneName = "";
                _techSkeletonNode?.SelectedBoneIndex(-1);
                SelectedBoneRotationOffset.Clear();
                SelectedBoneTranslationOffset.Clear();
            }
            else
            {
                var boneIndex = selectedBone.BoneIndex;
                var position = _techSkeletonNode.Skeleton.Translation[boneIndex];
                var rotation = _techSkeletonNode.Skeleton.Rotation[boneIndex];
                if (ShowBonesAsWorldTransform)
                {
                    var worldMatrix = _techSkeletonNode.Skeleton.GetWorldTransform(boneIndex);
                    worldMatrix.Decompose(out _, out rotation, out position);
                }

                var eulerRotation = MathUtil.QuaternionToEulerDegree(rotation);

                SelectedBoneName = SelectedBone.BoneName;
                _techSkeletonNode.SelectedBoneIndex(boneIndex);
                SelectedBoneRotationOffset.Set(eulerRotation);
                SelectedBoneTranslationOffset.Set(position);
            }

            SelectedBoneRotationOffset.DisableCallbacks = false;
            SelectedBoneTranslationOffset.DisableCallbacks = false;
        }

        void UpdateSelectedBoneName(string newName)
        {
            if (SelectedBone == null)
                return;

            SelectedBone.BoneName = newName;
            _techSkeletonNode.Skeleton.BoneNames[SelectedBone.BoneIndex] = newName;
        }

        private void SetTechSkeletonTransform(bool value)
        {
            if (value)
                _techSkeletonNode.Offset = Matrix.CreateScale(1, 1, -1);
            else
                _techSkeletonNode.Offset = Matrix.Identity;
        }

        private void HandleTranslationChanged()
        {
            try
            {
                BoneTransformHandler.Translate(SelectedBone,
                    _techSkeletonNode.Skeleton,
                    SelectedBoneTranslationOffset.GetAsVector3(),
                    SelectedBoneRotationOffset.GetAsVector3(),
                    ShowBonesAsWorldTransform);
            }
            catch (System.ArithmeticException)
            {
            }
            catch (System.Exception)
            {
            }
        }

        public void BakeSkeletonAction() => _techSkeletonNode.Skeleton.BakeScaleIntoSkeleton();

        public void FocusSelectedBoneAction()
        {
            if (SelectedBone == null)
                return;

            var worldPos = _techSkeletonNode.Skeleton.GetWorldTransform(SelectedBone.BoneIndex).Translation;
            FocusService.LookAt(worldPos);
        }

        public void CreateBoneAction()
        {
            if (SelectedBone == null)
                return;

            _techSkeletonNode.Skeleton.CreateChildBone(SelectedBone.BoneIndex);
            RefreshBoneList();
        }

        public void DuplicateBoneAction()
        {
            BoneManipulator.Duplicate(SelectedBone, _techSkeletonNode.Skeleton);
            RefreshBoneList();
        }

        public void DeleteBoneAction()
        {
            BoneManipulator.Delete(SelectedBone, _techSkeletonNode.Skeleton);
            RefreshBoneList();
        }

        public void CopyBoneAction()
        {
            BoneManipulator.Copy(_copyPasteManager, SelectedBone, _techSkeletonNode.Skeleton);
            RefreshBoneList();
        }

        public void PasteBoneAction()
        {
            BoneManipulator.Paste(_copyPasteManager, SelectedBone, _techSkeletonNode.Skeleton);
            RefreshBoneList();
        }

        public void SaveSkeletonAction()
        {
            if (_techSkeletonNode.Skeleton == null)
                return;

            if (_techSkeletonNode.Skeleton.HasBoneScale())
            {
                MessageBox.Show("Skeleton has scale, this needs to be baked before the skeleton can be saved");
                return;
            }

            var skeletonClip = AnimationClip.CreateSkeletonAnimation(_techSkeletonNode.Skeleton);
            var animFile = skeletonClip.ConvertToFileFormat(_techSkeletonNode.Skeleton);
            animFile.Header.SkeletonName = SourceSkeletonName;
            var animationBytes = AnimationFile.ConvertToBytes(animFile);

            var result = _packFileSaveService.Save(SkeletonName, animationBytes, false);
            SkeletonName = _packFileService.GetFullPath(result);

            var invMatrixFile = _techSkeletonNode.Skeleton.CreateInvMatrixFile();
            var invMatrixPath = Path.ChangeExtension(SkeletonName, ".bone_inv_trans_mats");
            _packFileSaveService.Save(invMatrixPath, invMatrixFile.GetBytes(), false);
        }

        public void LoadSkeletonAction()
        {
            var result = _packFileUiProvider.DisplayBrowseDialog([".anim"]);
            if (result.Result && result.File != null)
            {
                var path = _packFileService.GetFullPath(result.File);
                LoadSkeleton(_techSkeletonNode, path);
            }
        }

        public void LoadRefMeshAction()
        {
            var result = _packFileUiProvider.DisplayBrowseDialog([".variantmeshdefinition", ".wsmodel", ".rigid_model_v2"]);
            if (result.Result && result.File != null)
            {
                var file = result.File;
                SceneObjectEditor.SetMesh(_techSkeletonNode, file, false);
                RefMeshName = _packFileService.GetFullPath(file);
            }
        }
    }
}
