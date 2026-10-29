using GameWorld.Core.Test.TestUtility;
using GameWorld.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Test.Services;

public class ModelAssetResolverTests
{
    [Test]
    public void EffectiveMaterialUsesWsModelTexturesAndKeepsRmvFallbacks()
    {
        var rmvMaterial = RmvMaterialHelper.Create(ModelMaterialEnum.weighted);
        rmvMaterial.SetTexture(TextureType.Diffuse, "textures/rmv_diffuse.dds");
        rmvMaterial.SetTexture(TextureType.Normal, "textures/rmv_normal.dds");

        var wsMaterial = new WsModelMaterialFile
        {
            Alpha = true,
            Textures = new Dictionary<TextureType, string>
            {
                [TextureType.Diffuse] = "textures/ws_diffuse.dds"
            }
        };

        var resolved = ResolvedModelMaterial.Create(rmvMaterial, wsMaterial, "materials/body.material");

        Assert.That(resolved.UsesWsModelMaterial, Is.True);
        Assert.That(resolved.Alpha, Is.True);
        Assert.That(resolved.WsModelMaterialPath, Is.EqualTo("materials/body.material"));
        Assert.That(resolved.Textures[TextureType.Diffuse], Is.EqualTo("textures/ws_diffuse.dds"));
        Assert.That(resolved.Textures[TextureType.Normal], Is.EqualTo("textures/rmv_normal.dds"));
    }

    [Test]
    public void EffectiveMaterialWithoutWsModelUsesRmvTextures()
    {
        var rmvMaterial = RmvMaterialHelper.Create(ModelMaterialEnum.weighted);
        rmvMaterial.SetTexture(TextureType.Diffuse, "textures/rmv_diffuse.dds");

        var resolved = ResolvedModelMaterial.Create(rmvMaterial);

        Assert.That(resolved.UsesWsModelMaterial, Is.False);
        Assert.That(resolved.Alpha, Is.False);
        Assert.That(resolved.Textures[TextureType.Diffuse], Is.EqualTo("textures/rmv_diffuse.dds"));
    }

    [TestCase(0, false)]
    [TestCase(1, true)]
    public void EffectiveMaterialUsesRmvWeightedAlpha(int alphaValue, bool expectedAlpha)
    {
        var rmvMaterial = (WeightedMaterial)RmvMaterialHelper.Create(ModelMaterialEnum.weighted);
        rmvMaterial.IntParams.Set(WeightedParamterIds.IntParams_Alpha_index, alphaValue);

        var resolved = ResolvedModelMaterial.Create(rmvMaterial);

        Assert.That(resolved.HasExplicitAlpha, Is.True);
        Assert.That(resolved.Alpha, Is.EqualTo(expectedAlpha));
    }

    [Test]
    public void WsModelAlphaOverridesRmvWeightedAlpha()
    {
        var rmvMaterial = (WeightedMaterial)RmvMaterialHelper.Create(ModelMaterialEnum.weighted);
        rmvMaterial.IntParams.Set(WeightedParamterIds.IntParams_Alpha_index, 1);

        var resolved = ResolvedModelMaterial.Create(rmvMaterial, new WsModelMaterialFile { Alpha = false });

        Assert.That(resolved.HasExplicitAlpha, Is.True);
        Assert.That(resolved.Alpha, Is.False);
    }

    [Test]
    public void EffectiveMaterialLeavesUnknownRmvAlphaSafe()
    {
        var resolved = ResolvedModelMaterial.Create(RmvMaterialHelper.Create(ModelMaterialEnum.custom_terrain));

        Assert.That(resolved.HasExplicitAlpha, Is.False);
        Assert.That(resolved.Alpha, Is.False);
    }
}
