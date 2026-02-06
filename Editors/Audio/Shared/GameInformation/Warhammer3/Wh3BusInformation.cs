namespace Editors.Audio.Shared.GameInformation.Warhammer3
{
    // To find Bus IDs click on a Sound in the Audio Explorer and refer to the Bus of the lowest ActorMixer in the Graph (the top level Actor Mixer)
    // or as was the case with music where there is no graph use Wwiser to get the ActorMixer's and fine the OverrideBudIds there,
    // or just trial and error them to see what works.

    // We use the OverrideBusId to play audio through a given bus, where for example the ActorMixer hack doesn't work, as is the case with Music.
    public static class Wh3BusInformation
    {
        // Strangely the second from top Bus (3356399930) works but the top level Bus (3267614108) doesn't and neither does the third level bus (4042387584)
        public const uint Music = 3356399930;
    }
}
