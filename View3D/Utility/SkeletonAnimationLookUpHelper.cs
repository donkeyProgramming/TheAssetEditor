using Common;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace View3D.Utility
{
    public class SkeletonAnimationLookUpHelper
    {
        ILogger _logger = Logging.Create<SkeletonAnimationLookUpHelper>();
        Dictionary<string, List<PackFile>> _skeletonNameToAnimationMap = new Dictionary<string, List<PackFile>>();
        PackFileService _pf;

        List<string> _skeletonFilesNames = new List<string>();

        public SkeletonAnimationLookUpHelper(PackFileService packFileService)
        {
            _pf = packFileService;
            Initialize();
        }

        void Initialize()
        {
            _logger.Here().Information("Finding all animations");

            
            var AllAnimations = _pf.FindAllWithExtention(".anim");

            _logger.Here().Information("Animations found =" + AllAnimations.Count());

            foreach (var animation in AllAnimations)
            {
                try
                {
                    var animationSkeletonName = AnimationFile.GetAnimationHeader(animation).SkeletonName;
                    if (_skeletonNameToAnimationMap.ContainsKey(animationSkeletonName) == false)
                        _skeletonNameToAnimationMap.Add(animationSkeletonName, new List<PackFile>());

                    _skeletonNameToAnimationMap[animationSkeletonName].Add(animation);
                }
                catch (Exception e)
                {
                    _logger.Here().Error("Parsing failed for " + _pf.GetFullPath(animation)+ "\n" + e.ToString());
                }
            }


            _logger.Here().Information("Finding all skeletons");

            var allFilesInFolder = _pf.FindAllFilesInDirectory("animations\\skeletons");
            _skeletonFilesNames = allFilesInFolder.Where(x => Path.GetExtension(x.Name) == ".anim").Select(x => _pf.GetFullPath(x)).ToList();

            _logger.Here().Information("Skeletons found =" + _skeletonFilesNames.Count());

            _logger.Here().Information("Finding all done");
        }

        public List<string> GetAllSkeletonFileNames()
        {
            return _skeletonFilesNames;
        }

        public List<string> GetAllSkeletonNames()
        {
            return _skeletonFilesNames.Select(x => Path.GetFileNameWithoutExtension(x)).ToList();
        }

        public List<PackFile> GetAnimationsForSkeleton(string skeletonName)
        {
            if (_skeletonNameToAnimationMap.ContainsKey(skeletonName) == false)
                return new List<PackFile>();
            return _skeletonNameToAnimationMap[skeletonName];
        }

        public AnimationFile GetSkeletonFileFromName(string skeletonName)
        {
            foreach (var name in _skeletonFilesNames)
            {
                if (name.Contains(skeletonName))
                {
                    var file = _pf.FindFile(name);
                    if (file != null)
                        return AnimationFile.Create(file as PackFile);
                }
            }
            return null;
        }
    }
}
