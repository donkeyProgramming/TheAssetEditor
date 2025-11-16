using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectXTexNet;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel.Types;

namespace Editors.ImportExport.Common.Interfaces
{
    public interface IImageProcessor
    {
        ScratchImage Transform(ScratchImage scratchImage);
    }

}
