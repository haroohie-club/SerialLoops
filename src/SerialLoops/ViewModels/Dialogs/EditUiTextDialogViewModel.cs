using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Util;
using SerialLoops.Views.Dialogs;
using IO = SerialLoops.Lib.IO;

namespace SerialLoops.ViewModels.Dialogs;

public class EditUiTextDialogViewModel : ViewModelBase
{
    public ObservableCollection<UiTextWithDescription> UiTextStrings { get; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public EditUiTextDialogViewModel(Project project, ILogger log)
    {
        UiTextStrings = [];
        for (int i = 0; i < project.UiText.Messages.Count; i++)
        {
            if (!UiDescriptions.TryGetValue(i, out string text))
            {
                continue;
            }

            UiTextStrings.Add(new(project, text, i));
        }

        SaveCommand = ReactiveCommand.Create<EditUiTextDialog>(dialog =>
        {
            foreach (UiTextWithDescription uiText in UiTextStrings)
            {
                project.UiText.Messages[uiText.Index] = uiText.UiText.GetOriginalString(project);
            }
            IO.WriteStringFile(Path.Combine("assets", "data", $"{project.UiText.Index:X3}.s"),
                project.UiText.GetSource([]), project, log);
            dialog.Close();
        });
        CancelCommand = ReactiveCommand.Create<EditUiTextDialog>(dialog => dialog.Close());
    }

    private static readonly Dictionary<int, string> UiDescriptions = new()
    {
        { 2, Strings.Main_Topic },
        { 3, Strings.Haruhi_Topic },
        { 4, Strings.Character_Topic },
        { 5, Strings.Sub_Topic },
        { 6, Strings.Character_Distribution },
        { 7, Strings.Investigation_Phase_Results },
        { 8, Strings.Companion_Selection },
        { 9, Strings.Character_distribution_instructions },
        { 10, Strings.Title_screen_New_Game_ticker_tape },
        { 11, Strings.Title_screen_Load_Game_ticker_tape },
        { 12, Strings.Title_screen_Extra_ticker_tape },
        { 13, Strings.Title_screen_Options_ticker_tape },
        { 14, Strings.Pause_menu_Load_ticker_tape },
        { 15, Strings.Pause_menu_Config_ticker_tape },
        { 16, Strings.Pause_menu_Title_ticker_tape },
        { 17, Strings.Episode_1_title },
        { 18, Strings.Episode_2_title },
        { 19, Strings.Episode_3_title },
        { 20, Strings.Episode_4_title },
        { 21, Strings.Episode_5_title },
        { 22, Strings.Episode_1_ticker_tape },
        { 23, Strings.Episode_2_ticker_tape },
        { 24, Strings.Episode_3_ticker_tape },
        { 25, Strings.Episode_4_ticker_tape },
        { 26, Strings.Episode_5_ticker_tape },
        { 27, Strings.No_save_data_ticker_tape },
        { 28, Strings.No_saves_Load_Game_menu_ticker_tape },
        { 29, Strings.Load_Game_menu_ticker_tape },
        { 30, Strings.Save_game_ticker_tape },
        { 31, Strings.Yes },
        { 32, Strings.No },
        { 33, Strings.Load_this_save_prompt },
        { 34, Strings.Save_progress_prompt_message_box },
        { 35, Strings.Save_progress_prompt_end_game_message_box },
        { 36, Strings.Save_prompt },
        { 37, Strings.Overwrite_save_prompt_message_box },
        { 38, Strings.Loading_prompt_message_box },
        { 39, Strings.Saving_prompt_message_box },
        { 40, Strings.Accessing_save_data_prompt_message_box },
        { 41, Strings.Save_loaded_message_box },
        { 42, Strings.Game_saved_message_box },
        { 43, Strings.Title_screen_return_unsaved_progress_lost_prompt_message_box },
        { 44, Strings.Try_again_prompt_message_box },
        { 45, Strings.Save_progress_prompt_message_box },
        { 46, Strings.Resetting_save_data_message_box },
        { 47, Strings.Deleting_all_data_message_box },
        { 48, Strings.Save_game_read_fail_message_box },
        { 49, Strings.Save_game_write_fail_message_box },
        { 50, Strings.Save_data_damaged___reset_message_box },
        { 51, Strings.System_data_damaged___reset_message_box },
        { 52, Strings.Save_data_1_damaged___reset_message_box },
        { 53, Strings.Save_data_2_damaged___reset_message_box },
        { 54, Strings.Quicksave_data_damaged___reset_message_box },
        { 55, Strings.Companion_selection_description },
        { 56, Strings.Kyon_companion_selected_description },
        { 57, Strings.Asahina_companion_selected_description },
        { 58, Strings.Nagato_companion_selected_description },
        { 59, Strings.Koizumi_companion_selected_description },
        { 60, Strings.Puzzle_phase_character_description },
        { 61, Strings.Asahina_puzzle_phase_selected_description },
        { 62, Strings.Nagato_puzzle_phase_selected_description },
        { 63, Strings.Koizumi_puzzle_phase_selected_description },
        { 64, Strings.Sound__Options_ },
        { 65, Strings.Game_Investigation_Phase__Options_ },
        { 66, Strings.Game_Puzzle_Phase__Options_ },
        { 67, Strings.Reset_options_to_default_title },
        { 68, Strings.Erase_data_options_title },
        { 69, Strings.Sound_options_ticker_tape },
        { 70, Strings.Investigation_phase_options_ticker_tape },
        { 71, Strings.Puzzle_phase_options_ticker_tape },
        { 72, Strings.Reset_to_default_config_ticker_tape },
        { 73, Strings.Erase_data_options_ticker_tape },
        { 74, Strings.Background_music_volume_options_ticker_tape },
        { 75, Strings.Sound_effect_volume_options_ticker_tape },
        { 76, Strings.Unvoiced_dialogue_volume_options_ticker_tape },
        { 77, Strings.Voiced_dialogue_volume_options_ticker_tape },
        { 78, Strings.Character_voice_toggle_options_ticker_tape },
        { 79, Strings.Toggle_Kyon_s_voice_ticker_tape },
        { 80, Strings.Toggle_Haruhi_s_voice_ticker_tape },
        { 81, Strings.Toggle_Mikuru_s_voice_ticker_tape },
        { 82, Strings.Toggle_Nagato_s_voice_ticker_tape },
        { 83, Strings.Toggle_Koizumi_s_voice_ticker_tape },
        { 84, Strings.Toggle_Kyon_s_sister_s_voice_ticker_tape },
        { 85, Strings.Toggle_Tsuruya_s_voice_ticker_tape },
        { 86, Strings.Toggle_Taniguchi_s_voice_ticker_tape },
        { 87, Strings.Toggle_Kunikida_s_voice_ticker_tape },
        { 88, Strings.Toggle_Mysterious_Girl_s_voice_ticker_tape },
        { 89, Strings.Reset_settings_to_default_ticker_tape },
        { 90, Strings.All_data_will_be_erased_prompt_message_box },
        { 91, Strings.Second_prompt_on_data_being_erased },
        { 92, Strings.Data_reset_message_box },
        { 93, Strings.All_data_erased_message_box },
        { 94, Strings.Batch_Dialogue_Display_option },
        { 95, Strings.Dialogue_Skipping_setting },
        { 96, Strings.Puzzle_Interrupt_Scenes_setting },
        { 97, Strings.Topic_Stock_Mode_option },
        { 98, Strings.Batch_Dialogue_Display_ticker_tape },
        { 99, Strings.Dialogue_Skipping_ticker_tape },
        { 100, Strings.Puzzle_Interrupt_Scenes_ticker_tape },
        { 101, Strings.Topic_Stock_Mode_ticker_tape },
        { 102, Strings.Batch_Dialogue_Display_Off },
        { 103, Strings.Batch_Dialogue_Display_On },
        { 104, Strings.Puzzle_Interrupt_Scenes_Off },
        { 105, Strings.Puzzle_Interrupt_Scenes_Unseen_Only },
        { 106, Strings.Puzzle_Interrupt_Scenes_On },
        { 107, Strings.Dialogue_Skipping_Fast_Forward },
        { 108, Strings.Dialogue_Skipping_Skip_Already_Read },
        { 109, Strings.Kyon_s_Dialogue_Box_Group_Selection },
        { 111, Strings.Group_selection_impossible_selection_made_ticker_tape },
        { 112, Strings.Kyon_s_Dialogue_Box_Companion_Selection },
        { 115, Strings.Chess_Mode_Unlocked_message_box },
        { 116, Strings.Haruhi_Suzumiya_event_unlocked_message_box },
        { 117, Strings.Mikuru_Asahina_event_unlocked_message_box },
        { 118, Strings.Yuki_Nagato_event_unlocked_message_box },
        { 119, Strings.Itsuki_Koizumi_event_unlocked_message_box },
        { 120, Strings.Tsuruya_event_unlocked_message_box },
        { 121, Strings.Collected_all_Haruhi_topics_message_box },
        { 122, Strings.Collected_all_Mikuru_topics_message_box },
        { 123, Strings.Collected_all_Nagato_topics_message_box },
        { 124, Strings.Collected_all_Koizumi_topics_message_box },
        { 125, Strings.Collected_all_main_topics_message_box },
        { 126, Strings.Cleared_all_chess_puzzles_message_box },
        { 127, Strings._100__d_game_message_box },
        { 128, Strings.Extras_unlocked_message_box },
        { 129, Strings.Mystery_girl_voice_added_to_config_message_box },
    };
}

public class UiTextWithDescription(Project project, string description, int index) : ReactiveObject
{
    public string Description { get; } = description;

    [Reactive]
    public string UiText { get; set; } = project.UiText.Messages[index].GetSubstitutedString(project);

    public int Index { get; } = index;
}
