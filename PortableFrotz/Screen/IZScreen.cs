﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Frotz;

using zword = System.UInt16;
using zbyte = System.Byte;

namespace Frotz.Screen {


    public interface IZScreen {
        void HandleFatalError(String Message);
        ScreenMetrics GetScreenMetrics();
        void DisplayChar(char c);
        void RefreshScreen(); // TODO Need to make this a little different
        void SetCursorPosition(int x, int y);
        void ScrollLines(int top, int height, int lines);
        event EventHandler<ZKeyPressEventArgs> KeyPressed;
        void SetTextStyle(int new_style);
        void Clear();
        void ClearArea(int top, int left, int bottom, int right);

        System.IO.Stream OpenExistingFile(String defaultName, String Title, String Filter);
        System.IO.Stream OpenNewOrExistingFile(String defaultName, String Title, String Filter, String defaultExtension);
        String SelectGameFile(String filename, out byte[] filedata);

        ZSize GetImageInfo(byte[] Image);

        void ScrollArea(int top, int bottom, int left, int right, int units);

        void DrawPicture(int picture, byte[] Image, int y, int x);

        void SetFont(int font);

        void DisplayMessage(String Message, String Caption);

        int GetStringWidth(String s, CharDisplayInfo Font);

        void RemoveChars(int count);

        bool GetFontData(int font, ref zword height, ref zword width);

        void GetColor(out int foreground, out int background);
        void SetColor(int new_foreground, int new_background);

        zword PeekColor();



        void FinishWithSample(int number);
        void PrepareSample(int number);
        void StartSample(int number, int volume, int repeats, zword eos);
        void StopSample(int number);

        void SetInputMode(bool InputMode, bool CursorVisibility, bool SingleKey);

        void SetInputColor();
        void addInputChar(char c);

        void StoryStarted(String StoryName, Blorb.BlorbFile BlorbFile, int Version);
        
        ZPoint GetCursorPosition();

        void SetActiveWindow(int win);
        void SetWindowSize(int win, int top, int left, int height, int width);

        bool ShouldWrap();

        void Restarting();

    }
}