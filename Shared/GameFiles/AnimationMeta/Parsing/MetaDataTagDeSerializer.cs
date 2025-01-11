using System.Reflection;
using Shared.Core.ByteParsing;

namespace Shared.GameFormats.AnimationMeta.Parsing
{
    public class MetaDataTagDeSerializer
    {
        private readonly Dictionary<string, List<Type>> _typeTable = [];
        private readonly Dictionary<string, string> _descriptionMap = [];

        public MetaDataTagDeSerializer()
        {
            EnsureMappingTableCreated();
        }

        void EnsureMappingTableCreated()
        {
            CreateDescriptions();

            var typesWithMyAttribute =
                from a in AppDomain.CurrentDomain.GetAssemblies()
                from t in a.GetTypes()
                let attributes = t.GetCustomAttributes(typeof(MetaDataAttribute), true)
                where attributes != null && attributes.Length > 0
                select new { Type = t, Attributes = attributes.Cast<MetaDataAttribute>() };

            var typesWithMyAttributeList = typesWithMyAttribute.Select(x => new { x.Type, AttributeInfo = x.Attributes.First() })
                .OrderBy(x => x.AttributeInfo.Priority)
                .ToList();

            foreach (var instance in typesWithMyAttributeList)
            {
                var type = instance.Type;
                var key = instance.AttributeInfo.VersionName;
                if (_typeTable.ContainsKey(key) == false)
                    _typeTable.Add(key, new List<Type>());

                _typeTable[key].Add(type);

                var orderedPropertiesList = type.GetProperties()
                    .Where(x => x.CanWrite)
                    .Where(x => Attribute.IsDefined(x, typeof(MetaDataTagAttribute)))
                    .OrderBy(x => x.GetCustomAttributes<MetaDataTagAttribute>(false).Single().Order)
                    .Select(x => x.GetCustomAttributes<MetaDataTagAttribute>(false).Single());

                var allNumbers = orderedPropertiesList.Select(x => x.Order).ToArray();
                if (IsSequential(allNumbers) == false)
                    throw new Exception("Invalid ids");

                // Ensure we have a decription
                GetDescription(instance.AttributeInfo.Name);
            }

#if DEBUG
            // Check that we can create an instance
            foreach (var type in _typeTable.Keys)
            {
                var instance = CreateDefault(type);

                var t = instance.GetType();
                var props = t.GetProperties();

                foreach (var prop in props)
                {
                    if (prop.Name.ToLower() == "data")
                        continue;

                    var value = prop.GetValue(instance);
                    if (value == null)
                        throw new Exception($"{type} contains null value for attribute {prop.Name}");
                }
            }
#endif
        }

        void CreateDescriptions()
        {
            _descriptionMap["TIME"] = "Generic time marker";
            _descriptionMap["DISABLE_PERSISTENT"] = "Disable persistent animation metadata";
            _descriptionMap["DISABLE_PERSISTENT_VFX"] = "Disable persistent vfx metadata";
            _descriptionMap["DISABLE_FACIAL"] = "Disable facial animations";
            _descriptionMap["DISABLE_HEAD_TRACKING"] = "Disable head tracking";
            _descriptionMap["DISABLE_MODEL"] = "Disable model draw.";
            _descriptionMap["SNIP"] = "Snip the starting and ending frame of an animation";
            _descriptionMap["FULL_BODY"] = "Indicates that this animation should be full body, that is, not torso spliced";
            _descriptionMap["NOT_BUILDING"] = "Indicates that this animation cannot be used for attacking buildings";
            _descriptionMap["USE_BASE_METADATA"] = "Force the use of metadata in the base anim slot during a hardcoded splice. All metadata in the hardcoded splice slot is ignored.";
            _descriptionMap["IGNORE_FOOT_SLIDING"] = "Mark the animation as not affected by foot sliding, so don't do anything to try to correct it.";
            _descriptionMap["WEAPON_ON"] = "Weapon bone is displayed between start and end times; set value to weapon bone index";
            _descriptionMap["WEAPON_LHAND"] = "Weapon bone is spliced relative to left hand between start and end times; set value to weapon bone index";
            _descriptionMap["WEAPON_RHAND"] = "Weapon bone is spliced relative to right hand between start and end times; set value to weapon bone index";
            _descriptionMap["WEAPON_HIP"] = "Weapon bone is spliced relative to hip between start and end times; set value to weapon bone index";
            _descriptionMap["DOCK_EQPT_LHAND"] = "missing";
            _descriptionMap["DOCK_EQPT_RHAND"] = "missing";
            _descriptionMap["DOCK_EQPT_LHAND_2"] = "missing"; //TODO
            _descriptionMap["DOCK_EQPT_RHAND_2"] = "missing"; //TODO
            _descriptionMap["DOCK_EQPT_LWAIST"] = "missing";
            _descriptionMap["DOCK_EQPT_RWAIST"] = "missing";
            _descriptionMap["DOCK_EQPT_BACK"] = "Weapon bone is spliced relative to spine2 between start and end times; set value to weapon bone index";
            _descriptionMap["BLEND_OVERRIDE"] = "Override the blend method and blend time";
            _descriptionMap["DISABLE_PERSISTENT_ID"] = "Disable persistent metadata with a particular ID";
            _descriptionMap["MIN_TARGET_SIZE"] = "Can only use this animation against target larger or equal to this size";
            _descriptionMap["MAX_TARGET_SIZE"] = "Can only use this animation against target smaller or equal to this size";
            _descriptionMap["RIDER_CUSTOM_ANIMATION"] = "force the rider to play a custom animation";
            _descriptionMap["BEARING"] = "Generic bearing in degrees";
            _descriptionMap["DISTANCE"] = "Distance in cm; currently used for jump animations";
            _descriptionMap["IMPACT_SPEED"] = "On attack animations, speed of the strike; On death animations, speed change threshold to trigger animation";
            _descriptionMap["SC_RADIUS"] = "Change the soft collision radius (multiplier)";
            _descriptionMap["SC_HEIGHT"] = "Change the soft collision height (multiplier)";
            _descriptionMap["SC_RATIO"] = "Change the soft collision ratio";
            _descriptionMap["ALPHA"] = "Change the alpha value";
            _descriptionMap["CAMERA_SHAKE_SCALE"] = "Set the camera shake scale";
            _descriptionMap["RIDER_IDLE_SPEED_SCALE"] = "scale the rider animation speed with some factor";
            _descriptionMap["RESCALE"] = "rescale character to target scale";
            _descriptionMap["ALLOWED_DELTA_SCALE"] = "maximum allowed delta scale to use the animation";
            _descriptionMap["PERSISTENT_SPEED_SCALE"] = "change the speed of the persistent metadata";
            _descriptionMap["BOUNDING_VOLUME_OVERRIDE"] = "A scale factor for the bounding sphere. This is a workaround for if the character animates far from the origin (or spreads out) and is being culled by the camera.";
            _descriptionMap["POSITION"] = "Generic position";
            _descriptionMap["FIRE_POS"] = "Position where projectile is created; start time is the time of projectile spawn";
            _descriptionMap["IMPACT_POS"] = "Position where impact originates; start time is the time of impact";
            _descriptionMap["IMPACT_DIRECTION_POS"] = "TODO";
            _descriptionMap["TARGET_POS"] = "Indicates position of target";
            _descriptionMap["CAMERA_SHAKE_POS"] = "Start a camera shake at position";
            _descriptionMap["WOUNDED_POSE"] = "Specify the wounded pose in the last frame";
            _descriptionMap["LHAND_POSE"] = "Left hand pose key";
            _descriptionMap["RHAND_POSE"] = "Right hand pose key";
            _descriptionMap["FACE_POSE"] = "Face pose key";
            _descriptionMap["DISMEMBER"] = "Triggers dismembering at the start time";
            _descriptionMap["SPLICE"] = "Splices animation specified in metadata to this animation. Weights range from 0 to 1.";
            _descriptionMap["SPLICE_OVERRIDE"] = "Override hardcoded splice. Weights range from 0 to 1.";
            _descriptionMap["TRANSFORM"] = "Transform a node. Optionally override with another bone";
            _descriptionMap["CREW_LOCATION"] = "Position and face artillery crew";
            _descriptionMap["EFFECT"] = "Trigger a particle effect at a location relative to a node index";
            _descriptionMap["BLOOD"] = "Trigger a blood effect at a location relative to a node index";
            _descriptionMap["VOLUMETRIC_EFFECT"] = "Trigger a volumetric particle effect group";
            _descriptionMap["PROP"] = "Display a prop model at a location relative to a node index";
            _descriptionMap["ANIMATED_PROP"] = "Display an animated prop model at a location relative to a node index";
            _descriptionMap["RIDER_ATTACHMENT"] = "Mark an attachment point for a rider";
            _descriptionMap["TURRET_ATTACHMENT"] = "Mark an attachment point for a turret";
            _descriptionMap["SPLASH_ATTACK"] = "Trigger a splash attack and mark the area of effect";
            _descriptionMap["SOUND_IMPACT"] = "Time and with attack and defend types.";
            _descriptionMap["SOUND_ATTACK_TYPE"] = "Attack type of this combat animation.";
            _descriptionMap["SOUND_DEFEND_TYPE"] = "Defend type of this combat animation.";
            _descriptionMap["SOUND_SPHERE_LINK"] = "Link this entity with the spheres of matched combatants.";
            _descriptionMap["SOUND_BUILDING"] = "Time, event and position of sound to be triggered";
            _descriptionMap["SOUND_TRIGGER"] = "Time and type of sound to be triggered";
            _descriptionMap["SOUND_SPHERE"] = "Time and type of sound sphere to be triggered";
            _descriptionMap["SOUND_SPHERE_LINK"] = "TODO";
            _descriptionMap["SOUND_IMPACT"] = "TODO";
            _descriptionMap["POSITION"] = "TODO";
            _descriptionMap["SOUND_POSITION"] = "TODO";
            _descriptionMap["SYNC_MARKER"] = "TODO";
            _descriptionMap["VOLUMETRIC_EFFECT"] = "TODO";
            _descriptionMap["TURRET_ATTACHMENT"] = "TODO";
            _descriptionMap["PARENT_CONSTRAINT"] = "TODO";
            _descriptionMap["CANNOT_DISMEMBER"] = "mark an attack as not causing dismemberment";
            _descriptionMap["ALLOW_LEG_DISMEMBER"] = "TODO";
            _descriptionMap["ALLOW_FRONT_LEG_DISMEMBER"] = "TODO";
            _descriptionMap["CAMPAIGN_DISMEMBER"] = "TODO";
            _descriptionMap["FACEFX"] = "TODO";
            _descriptionMap["PULL_ROPE"] = "TODO";
            _descriptionMap["CROP_MARKER"] = "TODO";
            _descriptionMap["NOT_HERO_TARGET"] = "TODO";
            _descriptionMap["DIE_PERMANENTLY"] = "TODO";
            _descriptionMap["DISABLE_ENEMY_COLLISION"] = "TODO";
            _descriptionMap["MARK_RIGHT_FOOT_FRONT"] = "TODO";
            _descriptionMap["NO CHAINING"] = "TODO";
            _descriptionMap["NOT_REGULAR_TARGET"] = "TODO";
            _descriptionMap["START_CLIMB"] = "TODO";
            _descriptionMap["MATERIAL_FLAG"] = "TODO";
            _descriptionMap["FREEZE_WEAPON"] = "TODO";
            _descriptionMap["USE_BASE_METADATA"] = "Force the use of metadata in the base anim slot during a hardcoded splice. All metadata in the hardcoded splice slot is ignored.";
            _descriptionMap["RIDER_ANIMATION_REQUIRED"] = "Mark the mount animation as needing a synced rider animation is corresponding rider slot.";
            _descriptionMap["SHADER_PARAMETER"] = "Modify specified shader parameter. Blends between two closest values.";
            _descriptionMap["EJECT_ATTACHED"] = "Eject the attached riders. Defines the direction of ejection(2D, Y is ignored).";
        }

        public string GetDescription(string metaDataTagName)
        {
            if (_descriptionMap.ContainsKey(metaDataTagName) == false)
                throw new Exception($"Unable to get description of {metaDataTagName}");
            return _descriptionMap[metaDataTagName];
        }

        public string GetDescriptionSafe(string metaDataTagName)
        {
            if (_descriptionMap.ContainsKey(metaDataTagName) == false)
                return "Missing";
            return _descriptionMap[metaDataTagName];
        }

        public List<string> GetSupportedTypes()
        {
            return _typeTable.Select(x => x.Key).ToList();
        }

        bool IsSequential(int[] array)
        {
            return array.Zip(array.Skip(1), (a, b) => a + 1 == b).All(x => x);
        }

        List<Type> GetTypesFromMeta(BaseMetaEntry entry)
        {
            var key = entry.Name + "_" + entry.Version;
            if (_typeTable.ContainsKey(key) == false)
                return null;

            return _typeTable[key];
        }

        public BaseMetaEntry? DeSerialize(UnknownMetaEntry entry, out string? errorMessage)
        {
            var entryInfoList = GetEntryInformation(entry);
            if (entryInfoList == null)
            {
                errorMessage = $"Unable to find decoder for {entry.Name}_{entry.Version}";
                return null;
            }

            errorMessage = null;
            foreach (var entryInfo in entryInfoList)
            {
                var instance = Activator.CreateInstance(entryInfo.Type);
                var bytes = entry.Data;
                var currentIndex = 0;
                foreach (var proptery in entryInfo.Properties)
                {
                    var parser = ByteParserFactory.Create(proptery.PropertyType);
                    try
                    {
                        var value = parser.GetValueAsObject(bytes, currentIndex, out var bytesRead);
                        currentIndex += bytesRead;
                        proptery.SetValue(instance, value);
                        errorMessage = "";
                    }
                    catch (Exception e)
                    {
                        errorMessage = $"Failed to read object - {e.Message} bytes left";
                        break;
                    }
                }

                if (errorMessage != "")
                    continue;

                var bytesLeft = bytes.Length - currentIndex;
                if (bytesLeft != 0)
                {
                    errorMessage = $"Failed to read object - {bytesLeft} bytes left";
                    continue;
                }

                var typedInstance = instance as BaseMetaEntry;
                typedInstance.Name = entry.Name;
                typedInstance.Data = bytes;
                errorMessage = null;
                return typedInstance;
            }

            return null;
        }

        public List<(string Header, string Value)>? DeSerializeToStrings(BaseMetaEntry entry, out string? errorMessage)
        {
            var entryInfoList = GetEntryInformation(entry);
            if (entryInfoList == null)
            {
                errorMessage = $"Unable to find decoder for {entry.Name}_{entry.Version}";
                return null;
            }

            var bytes = entry.Data;
            var currentIndex = 0;
            var output = new List<(string, string)>();
            errorMessage = null;

            foreach (var entryInfo in entryInfoList)
            {
                errorMessage = "";
                foreach (var proptery in entryInfo.Properties)
                {
                    var parser = ByteParserFactory.Create(proptery.PropertyType);
                    var result = parser.TryDecode(bytes, currentIndex, out var value, out var bytesRead, out var error);
                    if (result == false)
                    {
                        errorMessage = $"Failed to serialize {proptery.Name} - {error}";
                        break;
                    }
                    currentIndex += bytesRead;
                    output.Add((proptery.Name, value));
                }

                if (errorMessage != "")
                    continue;

                var bytesLeft = bytes.Length - currentIndex;
                if (bytesLeft != 0)
                {
                    errorMessage = $"Failed to read object - {bytesLeft} bytes left";
                    continue;
                }

                errorMessage = null;
                return output;
            }

            return output;
        }

        public BaseMetaEntry CreateDefault(string itemName)
        {
            if (_typeTable.ContainsKey(itemName) == false)
                throw new Exception("Unknown metadata item " + itemName);

            var type = _typeTable[itemName].First();
            var instance = Activator.CreateInstance(type) as BaseMetaEntry;

            var itemNameSplit = itemName.ToUpper().Split("_");
            instance.Version = int.Parse(itemNameSplit.Last());
            instance.Name = string.Join("_", itemNameSplit.Take(itemNameSplit.Length - 1));
            return instance;
        }

        List<EntryInfoResult>? GetEntryInformation(BaseMetaEntry entry)
        {
            var metaDataTypes = GetTypesFromMeta(entry);
            if (metaDataTypes == null)
                return null;

            var output = new List<EntryInfoResult>();
            foreach (var metaDataType in metaDataTypes)
            {
                var instance = Activator.CreateInstance(metaDataType);
                var orderedPropertiesList = metaDataType.GetProperties()
                    .Where(x => x.CanWrite)
                    .Where(x => Attribute.IsDefined(x, typeof(MetaDataTagAttribute)))
                    .OrderBy(x => x.GetCustomAttributes<MetaDataTagAttribute>(false).Single().Order)
                    .ToList();

                var entryInfo = new EntryInfoResult() { Type = metaDataType, Properties = orderedPropertiesList };
                output.Add(entryInfo);
            }

            return output;
        }

        public class EntryInfoResult
        {
            public Type? Type { get; set; }
            public List<PropertyInfo> Properties { get; set; } = new();
        }
    }
}
