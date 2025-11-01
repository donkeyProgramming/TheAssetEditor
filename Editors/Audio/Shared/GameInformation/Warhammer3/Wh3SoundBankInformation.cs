using System;
using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.Shared.GameInformation.Warhammer3
{
    public enum Wh3SoundBank
    {
        None,
        BattleAdvice,
        BattleIndividualMagic,
        CampaignAdvice,
        CampaignDiplomacy,
        GlobalMovies,
        BattleVOGeneralsSpeech,
        GlobalMusic,
        FrontendVO,
        CampaignVO,
        CampaignVOConversational,
        BattleVO,
        BattleVOConversational,
    }

    public enum Wh3SoundBankEventType
    {
        ActionEvent,
        DialogueEvent
    }

    public record Wh3SoundBankDefinition(string Name, Wh3SoundBank GameSoundBank, Wh3Language? RequiredLanguage = null);

    public class Wh3SoundBankInformation
    {
        public static List<Wh3SoundBankDefinition> Information { get; } =
        [
            new("battle_advice", Wh3SoundBank.BattleAdvice),
            new("battle_individual_magic", Wh3SoundBank.BattleIndividualMagic),
            new("battle_vo_generals_speech", Wh3SoundBank.BattleVOGeneralsSpeech),
            new("campaign_advice", Wh3SoundBank.CampaignAdvice),
            new("campaign_diplomacy", Wh3SoundBank.CampaignDiplomacy),
            new("global_movies", Wh3SoundBank.GlobalMovies),
            new("global_music", Wh3SoundBank.GlobalMusic, Wh3Language.Sfx),
            new("frontend_vo", Wh3SoundBank.FrontendVO),
            new("campaign_vo", Wh3SoundBank.CampaignVO),
            new("campaign_vo_conversational", Wh3SoundBank.CampaignVOConversational),
            new("battle_vo_orders", Wh3SoundBank.BattleVO),
            new("battle_vo_conversational", Wh3SoundBank.BattleVOConversational),
        ];

        public static Wh3SoundBank GetSoundBank(string name) => Information.First(definition => definition.Name == name).GameSoundBank;

        public static string GetName(Wh3SoundBank soundBank) => Information.First(definition => definition.GameSoundBank == soundBank).Name;

        public static Wh3Language? GetRequiredLanguage(Wh3SoundBank soundBank) => Information.First(definition => definition.GameSoundBank == soundBank).RequiredLanguage;

        public static string GetSoundBankNameFromPrefix(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var matchingDefinition = Wh3SoundBankInformation.Information
                .OrderByDescending(soundBankDefinition => soundBankDefinition.Name.Length)
                .FirstOrDefault(soundBankDefinition =>
                    value.StartsWith(soundBankDefinition.Name, StringComparison.OrdinalIgnoreCase));

            return matchingDefinition?.Name;
        }
    }
}
