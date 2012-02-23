namespace Symphonary
{
    public class NoteMatcher
    {
        // This does matching for the violin. We will need to switch to wildcards though to handle chords.

        /// <summary>
        /// Checks if a serial port string cooresponds to the given note number
        /// </summary>
        /// <param name="serialData"></param>
        /// <param name="noteNumber"></param>
        /// <returns></returns>
        /*public bool NoteMatches(string serialData, int noteNumber)
        {
            if (serialData.Length == 4)
            {
                if (noteNumber == 55 && serialData[0] == '0')
                    return true;
                else if (noteNumber == 56 && serialData[0] == '1')
                    return true;
                else if (noteNumber == 57 && serialData[0] == '2')
                    return true;
                else if (noteNumber == 58 && serialData[0] == '3')
                    return true;
                else if (noteNumber == 59 && serialData[0] == '4')
                    return true;
                else if (noteNumber == 60 && serialData[0] == '5')
                    return true;
                else if (noteNumber == 61 && serialData[0] == '6')
                    return true;
                else if (noteNumber == 62 && (serialData[0] == '7' || serialData[1] == '0'))
                    return true;
                else if (noteNumber == 63 && serialData[1] == '1')
                    return true;
                else if (noteNumber == 64 && serialData[1] == '2')
                    return true;
                else if (noteNumber == 65 && serialData[1] == '3')
                    return true;
                else if (noteNumber == 66 && serialData[1] == '4')
                    return true;
                else if (noteNumber == 67 && serialData[1] == '5')
                    return true;
                else if (noteNumber == 68 && serialData[1] == '6')
                    return true;
                else if (noteNumber == 69 && (serialData[1] == '7' || serialData[2] == '0'))
                    return true;
                else if (noteNumber == 70 && serialData[2] == '1')
                    return true;
                else if (noteNumber == 71 && serialData[2] == '2')
                    return true;
                else if (noteNumber == 72 && serialData[2] == '3')
                    return true;
                else if (noteNumber == 73 && serialData[2] == '4')
                    return true;
                else if (noteNumber == 74 && serialData[2] == '5')
                    return true;
                else if (noteNumber == 75 && serialData[2] == '6')
                    return true;
                else if (noteNumber == 76 && (serialData[2] == '7' || serialData[3] == '0'))
                    return true;
                else if (noteNumber == 77 && serialData[3] == '1')
                    return true;
                else if (noteNumber == 78 && serialData[3] == '2')
                    return true;
                else if (noteNumber == 79 && serialData[3] == '3')
                    return true;
                else if (noteNumber == 80 && serialData[3] == '4')
                    return true;
                else if (noteNumber == 81 && serialData[3] == '5')
                    return true;
                else if (noteNumber == 82 && serialData[3] == '6')
                    return true;
                else if (noteNumber == 83 && serialData[3] == '7')
                    return true;

            }
            return false;
        }*/

        //Guitar Code
        public bool NoteMatches(string serialData, int noteNumber)
        {
            if (serialData.Length == 6) {
                if (noteNumber == 40 && serialData[0] == 'A')
                    return true;
                else if (noteNumber == 41 && serialData[0] == 'B')
                    return true;
                else if (noteNumber == 42 && serialData[0] == 'C')
                    return true;
                else if (noteNumber == 43 && serialData[0] == 'D')
                    return true;
                else if (noteNumber == 44 && serialData[0] == 'E')
                    return true;
                else if (noteNumber == 45 && serialData[0] == 'F' || serialData[1] == 'A')
                    return true;
                else if (noteNumber == 46 && serialData[0] == 'G' || serialData[1] == 'B')
                    return true;
                else if (noteNumber == 47 && serialData[0] == 'H' || serialData[1] == 'C')
                    return true;
                else if (noteNumber == 48 && serialData[0] == 'I' || serialData[1] == 'D')
                    return true;
                else if (noteNumber == 49 && serialData[0] == 'J' || serialData[1] == 'E')
                    return true;
                else if (noteNumber == 50 && serialData[0] == 'K' || serialData[1] == 'F' || serialData[2] == 'A')
                    return true;
                else if (noteNumber == 51 && serialData[0] == 'L' || serialData[1] == 'G' || serialData[2] == 'B')
                    return true;
                else if (noteNumber == 52 && serialData[0] == 'M' || serialData[1] == 'H' || serialData[2] == 'C')
                    return true;
                else if (noteNumber == 53 && serialData[0] == 'N' || serialData[1] == 'I' || serialData[2] == 'D')
                    return true;
                else if (noteNumber == 54 && serialData[0] == 'O' || serialData[1] == 'J' || serialData[2] == 'E')
                    return true;
                else if (noteNumber == 55 && serialData[0] == 'P' || serialData[1] == 'K' || serialData[2] == 'F' || serialData[3] == 'A')
                    return true;
                else if (noteNumber == 56 && serialData[0] == 'Q' || serialData[1] == 'L' || serialData[2] == 'G' || serialData[3] == 'B')
                    return true;
                else if (noteNumber == 57 && serialData[1] == 'M' || serialData[2] == 'H' || serialData[3] == 'C')
                    return true;
                else if (noteNumber == 58 && serialData[1] == 'N' || serialData[2] == 'I' || serialData[3] == 'D')
                    return true;
                else if (noteNumber == 59 && serialData[1] == 'O' || serialData[2] == 'J' || serialData[3] == 'E' || serialData[4] == 'A')
                    return true;
                else if (noteNumber == 60 && serialData[1] == 'P' || serialData[2] == 'K' || serialData[3] == 'F' || serialData[4] == 'B')
                    return true;
                else if (noteNumber == 61 && serialData[1] == 'Q' || serialData[2] == 'L' || serialData[3] == 'G' || serialData[4] == 'C')
                    return true;
                else if (noteNumber == 62 && serialData[2] == 'M' || serialData[3] == 'H' || serialData[4] == 'D')
                    return true;
                else if (noteNumber == 63 && serialData[2] == 'N' || serialData[3] == 'I' || serialData[4] == 'E')
                    return true;
                else if (noteNumber == 64 && serialData[2] == 'O' || serialData[3] == 'J' || serialData[4] == 'F' || serialData[5] == 'A')
                    return true;
                else if (noteNumber == 65 && serialData[2] == 'P' || serialData[3] == 'K' || serialData[4] == 'G' || serialData[5] == 'B')
                    return true;
                else if (noteNumber == 66 && serialData[2] == 'Q' || serialData[3] == 'L' || serialData[4] == 'H' || serialData[5] == 'C')
                    return true;
                else if (noteNumber == 67 && serialData[3] == 'M' || serialData[4] == 'I' || serialData[5] == 'D')
                    return true;
                else if (noteNumber == 68 && serialData[3] == 'N' || serialData[4] == 'J' || serialData[5] == 'E')
                    return true;
                else if (noteNumber == 69 && serialData[3] == 'O' || serialData[4] == 'K' || serialData[5] == 'F')
                    return true;
                else if (noteNumber == 70 && serialData[3] == 'P' || serialData[4] == 'L' || serialData[5] == 'G')
                    return true;
                else if (noteNumber == 71 && serialData[3] == 'Q' || serialData[4] == 'M' || serialData[5] == 'H')
                    return true;
                else if (noteNumber == 72 && serialData[4] == 'N' || serialData[5] == 'I')
                    return true;
                else if (noteNumber == 73 && serialData[4] == 'O' || serialData[5] == 'J')
                    return true;
                else if (noteNumber == 74 && serialData[4] == 'P' || serialData[5] == 'K')
                    return true;
                else if (noteNumber == 75 && serialData[4] == 'Q' || serialData[5] == 'L')
                    return true;
                else if (noteNumber == 76 && serialData[5] == 'M')
                    return true;
                else if (noteNumber == 77 && serialData[5] == 'N')
                    return true;
                else if (noteNumber == 78 && serialData[5] == 'O')
                    return true;
                else if (noteNumber == 79 && serialData[5] == 'P')
                    return true;
                else if (noteNumber == 80 && serialData[5] == 'Q')
                    return true;

            }
            return false;
        }

        // this does note-checking for the flute, just ignore this for now as it is not being used

        /*public bool NoteMatches(string serialData, int noteNumber)
        {
            switch (serialData) {
                case "01111001110": // DN4***
                    return noteNumber == 50;
                case "01111001111": // DS4EF4
                    return noteNumber == 51;
                case "01111001101": // EN4FF4, EN5FF5
                    return noteNumber == 52 || noteNumber == 64;
                case "01111001001": // ES4FN4
                    return noteNumber == 53;
                case "01111000011": // FS4GF4, FS5GF5
                    return noteNumber == 54 || noteNumber == 66;
                case "01111000001": // GN4***, GN5***
                    return noteNumber == 55 || noteNumber == 67;
                case "01111010001": // GS4AF4, GS5AF5
                    return noteNumber == 56 || noteNumber == 68;
                case "01110000001": // AN4***, AN5***
                    return noteNumber == 57 || noteNumber == 69;
                case "01100001001": // AS4BF4
                    return noteNumber == 58;
                case "01100000001": // BN4CF5, BN5CF6
                    return noteNumber == 59 || noteNumber == 71;
                case "00100000001": // BS4CN5, BS5CN6
                    return noteNumber == 60 || noteNumber == 72;
                case "00000000001": // CS5DF5, CS6DF6
                    return noteNumber == 61 || noteNumber == 73;
                case "01011001110": // DN5***
                    return noteNumber == 62;
                case "01011001111": // DS5EF5
                    return noteNumber == 63;
                case "10100000001": // AS5BF5
                    return noteNumber == 70;
                case "01011000001": // DN6***
                    return noteNumber == 74;
                case "01111011111": // DS6EF6
                    return noteNumber == 75;
                case "01110001101": // EN6FF6
                    return noteNumber == 76;
                case "01101001001": // ES6FN6
                    return noteNumber == 77;
                case "01101000011": // FS6GF6
                    return noteNumber == 78;
                case "00111000001": // GN6***
                    return noteNumber == 79;
                case "00011010001": // GS6AF6
                    return noteNumber == 80;
                case "01010001001": // AN6***
                    return noteNumber == 81;
                default: // does not match the note
                    return false;
            }
        }*/

    } // end NoteMatcher

}