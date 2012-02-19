using System;
using System.Diagnostics;

namespace Symphonary
{
    public static class Transposer
    {
        public enum TransposeReturnStatus
        {
            AllNotesAlreadyInRange,
            TransposeSuccessful,
            TransposeUnsuccessful
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="notes"></param>
        /// <param name="deviceNoteUpperBound"></param>
        /// <param name="deviceNoteLowerBound"></param>
        /// <returns>A boolean value indicating if the transposition was successful</returns>
        public static TransposeReturnStatus Transpose(Note[] notes, int deviceNoteUpperBound, int deviceNoteLowerBound)
        {
            int highestNote = int.MaxValue;
            int lowestNote = int.MinValue;

            foreach (Note note in notes)
            {
                if (note.NoteNumber < highestNote)
                {
                    highestNote = note.NoteNumber;
                }
                if (note.NoteNumber > lowestNote)
                {
                    lowestNote = note.NoteNumber;
                }
            }

            Console.WriteLine("[Transpose] highestNote={0}, lowestNote={1}", highestNote, lowestNote);

            // we don't need to transpose
            if (highestNote >= deviceNoteUpperBound && lowestNote <= deviceNoteLowerBound)
            {
                return TransposeReturnStatus.AllNotesAlreadyInRange;
            }

            if ((lowestNote - highestNote) > (deviceNoteLowerBound - deviceNoteUpperBound))
            {
                return TransposeReturnStatus.TransposeUnsuccessful;
            }
            
            if (Math.Abs(deviceNoteUpperBound - highestNote) < 12)
            {
                return TransposeReturnStatus.TransposeUnsuccessful;
            }

            // get minimum offset required to have the highest note be in range
            int offset = 12*((deviceNoteUpperBound - highestNote + 11)/12);
            
            if (lowestNote + offset > deviceNoteLowerBound)
            {
                return TransposeReturnStatus.TransposeUnsuccessful;
            }

            foreach (Note note in notes)
            {
                note.NoteNumber += offset;
                Debug.Assert(note.NoteNumber >= deviceNoteUpperBound && note.NoteNumber <= deviceNoteLowerBound);
            }

            return TransposeReturnStatus.TransposeSuccessful;
        }
    }
}