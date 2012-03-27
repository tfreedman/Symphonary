using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Symphonary
{
    class GuitarNote : Note, IComparable
    {
        public int StringNumber;
        public int FretNumber;
        
        public GuitarNote(int stringNumber, int fretNumber, Note note)
            : base(note.NoteNumber, note.BeginTime, note.EndTime)
        {
            StringNumber = stringNumber;
            FretNumber = fretNumber;
        }

        // implement IComparable interface (this way you can do Array.Sort on a GuitarNote array
        public int CompareTo(object obj)
        {
            GuitarNote otherGuitarNote = obj as GuitarNote;
            if (otherGuitarNote != null)
            {
                return BeginTime.CompareTo(otherGuitarNote.BeginTime);
            }
            throw new Exception("[GuitarNote.CompareTo] Other object is not a GuitarNote");
        }
    }
    
    class StringAllocator
    {
        private int[,] guitar = new int[6, 2] { { 64, 80 }, { 59, 75 }, { 55, 71 }, { 50, 66 }, { 45, 61 }, { 40, 56 } };
        private int upperNoteBound = 40;
        private int lowerNoteBound = 80;
        
        private List<GuitarNote>[] alloc = new List<GuitarNote>[6];

        private GuitarNote[] allocSingleArr;
        public GuitarNote[] AllocSingleArr
        {
            get { return allocSingleArr; }
        }

        public int NumDroppedNotes;
        public int NumOutOfRangeNotes;

        public StringAllocator()
        {
            NumDroppedNotes = 0;
            NumOutOfRangeNotes = 0;
            for (int i = 0; i < alloc.Length; i++)
            {
                alloc[i] = new List<GuitarNote>();
            }
        }

        public void Clear()
        {
            NumDroppedNotes = 0;
            NumOutOfRangeNotes = 0;
            for (int i = 0; i < alloc.Length; i++)
            {
                alloc[i].Clear();
            }
            allocSingleArr = null;
        }

        public void AllocateNotes(Note[] notes)
        {
            foreach (Note note in notes)
            {
                AddNote(note);
            }

            allocSingleArr = new GuitarNote[notes.Length - NumDroppedNotes];
            int j = 0;
            for (int i = 0; i < alloc.Length; i++)
            {
                foreach (GuitarNote guitarNote in alloc[i])
                {
                    allocSingleArr[j] = guitarNote;
                    j++;
                }
            }
            Array.Sort(allocSingleArr);
        }

        private void AddNote(Note note)
        {
            bool success = false;
            
            if (note.NoteNumber >= upperNoteBound && note.NoteNumber <= lowerNoteBound)
            {
                for (int i = 0; i < alloc.Length; i++)
                {
                    if (guitar[i, 0] < note.NoteNumber && note.NoteNumber < guitar[i, 1] &&
                        IsFree(i, note.BeginTime, note.EndTime))
                    {
                        AddNote_Helper(i, note.NoteNumber - guitar[i, 0], note);
                        success = true;
                        break;
                    }
                }
            }
            else
            {
                NumOutOfRangeNotes++;
            }

            if (!success)
            {
                // Note dropped.
                NumDroppedNotes++;
            }
        }

        private void AddNote_Helper(int stringNumber, int fretNumber, Note note)
        {
            int i = 0;
            foreach (GuitarNote guitarNote in alloc[stringNumber])
            {
                if (note.BeginTime < guitarNote.BeginTime)
                {
                    alloc[stringNumber].Insert(i, new GuitarNote(stringNumber, fretNumber, note));
                    break;
                }
                i++;
            }
            if (i == alloc[stringNumber].Count)
            {
                alloc[stringNumber].Add(new GuitarNote(stringNumber, fretNumber, note));
            }
        }

        private bool IsFree(int stringNumber, long beginTime, long endTime)
        {
            foreach (GuitarNote guitarNote in alloc[stringNumber])
            {
                // check for overlap in time duration
                if (beginTime < guitarNote.BeginTime && guitarNote.BeginTime < endTime)
                {
                    return false;
                }
                if (beginTime < guitarNote.EndTime && guitarNote.EndTime < endTime)
                {
                    return false;
                }
                if (guitarNote.BeginTime < beginTime && beginTime < guitarNote.EndTime)
                {
                    return false;
                }
                if (guitarNote.BeginTime < endTime && endTime < guitarNote.EndTime)
                {
                    return false;
                }
            }
            return true;
        }
    }
}