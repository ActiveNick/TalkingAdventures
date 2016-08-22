using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Frotz.Other
{
    public enum GameFileTypes : short
    {
        ZBlorb,
        Regular,
        Other,
        ZipFile,
        Unknown,
        Error
    }

    public class GameFileInfo
    {
        public GameFileTypes Type { get; private set; }
        public byte[] Data { get; private set; }

        private GameFileInfo(GameFileTypes Type, byte[] Data) {
            this.Type = Type;
            this.Data = Data;
        }

        private static void CopyTo(System.IO.Stream Stream1, System.IO.Stream Stream2) {
            byte[] buffer = new byte[1000];

            int read;
            int pos = 0;

            while ( (read = Stream1.Read(buffer, 0, buffer.Length)) != 0) {
                Stream2.Write(buffer, pos, read);
                pos += read;
            }
        }
        
        public static GameFileInfo WhatFileType(System.IO.Stream s)
        {
            if (s.Length < 4) throw new ArgumentException("File array is to small");

            try
            {
                byte[] gamefile = null;
                System.IO.MemoryStream ms = s as System.IO.MemoryStream;
                if (ms != null) {
                    gamefile = ms.ToArray();
                } else {
                    ms = new System.IO.MemoryStream();
                }
                // CopyTo(s, ms);

                // var temp = ms.ToArray();

                if (gamefile[0] == (byte)'P' && gamefile[1] == (byte)'K')
                {
                    return new GameFileInfo(GameFileTypes.ZipFile, null);
                }

                if (s.Length > 2000000)
                {
                    throw new ArgumentException("Stream appears to be to large to be a game file");
                }

                // var gamefile = ms.ToArray();

                if (gamefile[0] == 'F' && gamefile[1] == 'O' && gamefile[2] == 'R' && gamefile[3] == 'M')
                {
                    var b = Frotz.Blorb.BlorbReader.ReadBlorbFile(gamefile);

                    return new GameFileInfo(GameFileTypes.ZBlorb, gamefile);
                }

                var info = ZTools.InfoDump.main(ref gamefile, new string[0]);

                return new GameFileInfo(GameFileTypes.Regular, gamefile);
            }
            catch
            {
                return new GameFileInfo(GameFileTypes.Error, null);
            }
            finally
            {
                s.Position = 0;
            }
        }


        public static GameFileTypes WhatFileType(byte[] gamefile)
        {
            if (gamefile.Length < 4) throw new ArgumentException("File array is to samll");

            if (gamefile[0] == (byte)'F' && gamefile[1] == (byte)'O' && gamefile[2] == (byte)'R' && gamefile[3] == (byte)'M')
            {
                var b = Frotz.Blorb.BlorbReader.ReadBlorbFile(gamefile);

                return GameFileTypes.ZBlorb;
            }

            if (gamefile[0] == (char)'P' && gamefile[1] == (char)'K')
            {
                return GameFileTypes.ZipFile;
            }

                var info = ZTools.InfoDump.main(ref gamefile, new string[0]);

            return GameFileTypes.Regular;
        }
    }
}
