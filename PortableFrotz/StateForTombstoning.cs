using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using Frotz.Generic;

namespace Frotz
{
    [KnownType(typeof(System.Collections.Generic.Dictionary<int, String>))]
    [KnownType(typeof(Frotz.Other.ZWindow[]))]
    [DataContract]
    public class SaveMachineState
    {
        private static SaveMachineState _sms = null;

        private static SaveMachineState _restoreState = null;

        public SaveMachineState() { }

        [DataMember]
        public byte[] StoryFile { get; set; }
        [DataMember]
        public String StoryFileName { get; set; }
        [DataMember]
        public int[] main_stack { get; set; }

        [DataMember] 
        public Dictionary<String, Object> MainMembers = new Dictionary<string, object>();
        [DataMember]
        public Dictionary<String, Object> FastMemMembers = new Dictionary<string, object>();
        [DataMember]
        public Dictionary<String, Object> ScreenMemebers = new Dictionary<string, object>();

        [DataMember]
        public int X { get; set; }
        
        [DataMember]
        public int Y { get; set; }
        
        [DataMember]
        public String[] Lines { get; set; }

        [DataMember]
        public List<List<PortableFrotz.Screen.ScrollingText>> ScrollBack { get; set; }

        private static void save_all_fields(Type t, Dictionary<String, Object> storage)
        {
            var fields = t.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

            foreach (var f in fields)
            {
                switch (f.Name)
                {
                    case "story_id": // Not a concern because it will be the same for the current story
                    case "records": // Not a concern because it's a constant struct
                    // Undo
                    case "undo_mem":
                    case "prev_zmp":
                    case "undo_diff":
                    case "undo_count":

                    case "current_window": // Unsure
                    case "mapper": // Static field
                    case "font_height": // Font height and width will be set by the z-machine and not change
                    case "font_width":

                    // saved through other considerations
                    case "stack":
                    case "story_fp": // TODO Not even sure this is necessary
                        break;
                    default:
                        storage.Add(f.Name, f.GetValue(null));
                        break;
                }
            }
        }

        private static void restore_all_fields(Type t, Dictionary<String, Object> storage)
        {
            var fields = t.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

            foreach (var f in fields)
            {
                if (storage.ContainsKey(f.Name))
                {
                    f.SetValue(null, storage[f.Name]);
                }
            }
        }

        public static void SaveState(String FileName)
        {
            _sms = new SaveMachineState();
            save_all_fields(typeof(main), _sms.MainMembers);

            save_all_fields(typeof(FastMem), _sms.FastMemMembers);

            _sms.StoryFileName = FileName;

            _sms.StoryFile = new byte[FastMem.story_fp.Length];
            long temp = FastMem.story_fp.Position;
            FastMem.story_fp.Position = 0;
            FastMem.story_fp.Read(_sms.StoryFile, 0, _sms.StoryFile.Length);
            FastMem.story_fp.Position = temp;

            _sms.main_stack = new int[main.stack.Length];
            for (int i = 0; i < main.stack.Length; i++)
            {
                _sms.main_stack[i] = main.stack[i];
            }

            save_all_fields(typeof(Frotz.Generic.Screen), _sms.ScreenMemebers);
        }

        public static SaveMachineState GetState()
        {
            return _sms;
        }

        public static void ClearState()
        {
            _sms = null;
        }

        public void RestoreState()
        {
            _restoreState = this;
        }

        public static SaveMachineState attemptRestore() {
            try
            {
                if (_restoreState != null && _restoreState.FastMemMembers != null)
                {
                    restore_all_fields(typeof(main), _restoreState.MainMembers);
                    restore_all_fields(typeof(FastMem), _restoreState.FastMemMembers);

                    restore_all_fields(typeof(Frotz.Generic.Screen), _restoreState.ScreenMemebers);

                    FastMem.story_fp = new System.IO.MemoryStream(_restoreState.StoryFile);

                    for (int i = 0; i < main.stack.Length; i++)
                    {
                        main.stack[i] = (ushort)_restoreState.main_stack[i];
                    }
                    return _restoreState;
                }

                return null;
            }
            finally
            {
                _restoreState = null;
            }
        }
    }
}
