using System.Text;
using Editors.VfxEditor.Document;
using GameWorld.Core.Vfx;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;

namespace Editors.VfxEditor
{
    /// <summary>The open file: its document, where it lives, and saving through the AssetEditor's save
    /// service. Save writes to this path in the editable pack, so a file opened from CA's packs is created there.</summary>
    public sealed class VfxFileSession(IPackFileService packFileService, IFileSaveService fileSaveService)
    {
        const string ReopenHint = "Close this tab and open the file again, picking the text editor.";

        public PackFile? File { get; private set; }
        public VfxDocument? Document { get; private set; }
        public string? LoadError { get; private set; }
        public bool HasUnsavedChanges { get; set; }

        public void Load(PackFile file)
        {
            File = file;
            Document = null;
            LoadError = null;
            HasUnsavedChanges = false;
            try
            {
                var document = VfxDocument.Load(file.DataSource.ReadData());
                if (!document.IsEffect)
                {
                    LoadError = $"{file.Name} is not an effect: it has no <vfx> and no <vfx_references>. {ReopenHint}";
                    return;
                }
                document.Changed += () => HasUnsavedChanges = true;
                Document = document;
            }
            catch (VfxParseException e)
            {
                LoadError = $"{file.Name} did not parse: {e.Message}. {ReopenHint}";
            }
            catch (DecoderFallbackException e)
            {
                LoadError = $"{file.Name} is not valid text in its encoding: {e.Message}. {ReopenHint}";
            }
        }

        /// <summary>Vanilla's layout of an effect with nothing in it, as CA's effect editor (Terry) writes one.</summary>
        const string EmptyEffect = "<shader_library file=\"fx/particle_vfx_library.hlsl\"/><vfx revision=\"2\" construction_type=\"2\" enable_in_ted=\"false\" camera_following=\"false\" ref_spawn_speed=\"-1.00\">\r\n" +
            "\t<!-- EMITTERS-->\r\n\t<emitters></emitters>\r\n\t<!-- BEHAVIOUR_CHAINS-->\r\n\t<behaviour_chains></behaviour_chains>\r\n" +
            "\t<!-- EFFECT RENDER STACKS-->\r\n\t<effect_render_stacks></effect_render_stacks>\r\n" +
            "</vfx><vfx_references></vfx_references><!-- EDITOR DATA--><editor_data>\r\n\t<effect_components></effect_components>\r\n</editor_data>";

        /// <summary>A new effect with no file yet, in UTF-16 as CA's are. Save asks where it goes.</summary>
        public void New()
        {
            File = null;
            LoadError = null;
            Document = VfxDocument.Load([0xFF, 0xFE, .. Encoding.Unicode.GetBytes(EmptyEffect)]);
            Document.Changed += () => HasUnsavedChanges = true;
            HasUnsavedChanges = true;
        }

        /// <summary>The name an EFFECT tag or a reference uses for this file: its path under vfx\ without ".xml".</summary>
        public string EffectName
        {
            get
            {
                var path = File == null ? "" : packFileService.GetFullPath(File).Replace('/', '\\');
                if (path.StartsWith("vfx\\", StringComparison.OrdinalIgnoreCase))
                    path = path[4..];
                return (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ? path[..^4] : path).Replace('\\', '/');
            }
        }

        public bool Save() => File == null ? SaveAs() : Store(fileSaveService.Save(packFileService.GetFullPath(File), Document!.Save(), prompOnConflict: false));

        public bool SaveAs() => Store(fileSaveService.SaveAs(".xml", Document!.Save()));

        bool Store(PackFile? saved)
        {
            if (saved == null)
                return false;
            File = saved;
            HasUnsavedChanges = false;
            return true;
        }

        /// <summary>The emitter ids of an effect in the loaded packs, for a new reference's override rows; null when the
        /// effect is not found or does not read.</summary>
        public IReadOnlyList<string>? EmitterIdsOf(string effectName)
        {
            var file = packFileService.FindFile(VfxResolver.PathOf(effectName));
            if (file == null)
                return null;
            try
            {
                var document = VfxDocument.Load(file.DataSource.ReadData());
                return document.Section("emitters")?.ChildrenNamed("emitter").Select(e => e.Attr("id") ?? "").ToList() ?? [];
            }
            catch (Exception)   // a pack moved on disk too, not only a file that does not parse
            {
                return null;
            }
        }
    }
}
