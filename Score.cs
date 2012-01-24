using System;

namespace Symphonary {
    public class Score {
        public int i_NumNotesScored = 0;
        public string s_CurrentFingering = string.Empty;

        NoteMatcher noteMatcher = new NoteMatcher();

        public void resetScore() {
            i_NumNotesScored = 0;
        }

        public void updateScore(ref MidiPlayer midiPlayer) {
            if (midiPlayer == null)
                return;

            // this is a hack, we shouldn't be modifying this list in the MidiPlayer, it should be used for
            // informational purposes
            for (int i = 0; i < midiPlayer.al_CurrentPlayingChannelNotes.Count; i++) {
                if (noteMatcher.noteMatches(s_CurrentFingering, (int)midiPlayer.al_CurrentPlayingChannelNotes[i])) {
                    i_NumNotesScored++;
                    midiPlayer.al_CurrentPlayingChannelNotes.RemoveAt(i);
                    break;
                }
            }
        }

        public string scoreGrade(int i_NumChannelNotesPlayed) {
            try {
                if (i_NumChannelNotesPlayed == 0) {
                    return "...";
                }
            } catch (NullReferenceException e) {
                return "...";
            }

            double percentage = ((double)i_NumNotesScored / (double)(i_NumChannelNotesPlayed)) * 100;

            if (percentage >= 90)
                return "A+";
            else if (percentage >= 85)
                return "A";
            else if (percentage >= 80)
                return "A-";
            else if (percentage >= 76)
                return "B+";
            else if (percentage >= 73)
                return "B";
            else if (percentage >= 70)
                return "B-";
            else if (percentage >= 67)
                return "C+";
            else if (percentage >= 63)
                return "C";
            else if (percentage >= 60)
                return "C-";
            else if (percentage >= 57)
                return "D+";
            else if (percentage >= 53)
                return "D";
            else if (percentage >= 50)
                return "D-";
            else
                return "F";
        }
    }
}
