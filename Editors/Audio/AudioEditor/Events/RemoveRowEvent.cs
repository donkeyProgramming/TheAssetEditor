using System.Collections.Generic;
using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record RemoveRowEvent(DataRow row);
}
