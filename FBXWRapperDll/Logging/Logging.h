/*
    File: Logging.h

    Win32 api console + txt file loggin

    Author: Phazer, 2020-2023    
*/

#pragma once

#include <Windows.h>
#include <fstream>
#include <string>
#include <iostream>
#include <fstream>
#include <locale>
#include <codecvt>
#include "..\HelperUtils\Tools.h"

#define FULL_FUNC_INFO(message) std::string(__func__) +  std::string(": Line: ") + std::to_string(__LINE__) + ": " + message

//#define LogActionColor(message) ;
//#define LogActionError(msg) false;
//#define LogInfo(msg) ;
//#define LogAction(message) ;
//#define LogActionWarning(message) false ;
//#define LogActionSuccess(message) ;



#define LogActionError(msg) ImplLog::LogActionErrorFalse( FULL_FUNC_INFO(msg) );

#define LogInfo(msg) ImplLog::LogActionInfo( FULL_FUNC_INFO(msg) );

#define LogAction(message)  ImplLog::LogActionInfo( \
	std::string(__func__) +  std::string(": Line: ") + std::to_string(__LINE__) + ": " + message);\

#define LogActionColor(message)  ImplLog::LogSimpleWithColor( \
	std::string(__func__) +  std::string(": Line: ") + std::to_string(__LINE__) + ": " + message);\

#define LogActionSuccess(message) ImplLog::LogAction_success(message);

#define LogActionWarning(message) ImplLog::LogAction_warning(message);

enum ConsoleColorFG
{
    FG_BLACK = 0,
    FG_DARKBLUE = FOREGROUND_BLUE,
    FG_DARKGREEN = FOREGROUND_GREEN,
    FG_DARKCYAN = FOREGROUND_GREEN | FOREGROUND_BLUE,
    FG_DARKRED = FOREGROUND_RED,
    FG_DARKMAGENTA = FOREGROUND_RED | FOREGROUND_BLUE,
    FG_DARKYELLOW = FOREGROUND_RED | FOREGROUND_GREEN,
    FG_DARKGRAY = FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE,
    FG_GRAY = FOREGROUND_INTENSITY,
    FG_BLUE = FOREGROUND_INTENSITY | FOREGROUND_BLUE,
    FG_GREEN = FOREGROUND_INTENSITY | FOREGROUND_GREEN,
    FG_CYAN = FOREGROUND_INTENSITY | FOREGROUND_GREEN | FOREGROUND_BLUE,
    FG_RED = FOREGROUND_INTENSITY | FOREGROUND_RED,
    FG_MAGENTA = FOREGROUND_INTENSITY | FOREGROUND_RED | FOREGROUND_BLUE,
    FG_YELLOW = FOREGROUND_INTENSITY | FOREGROUND_RED | FOREGROUND_GREEN,
    FG_WHITE = FOREGROUND_INTENSITY | FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE,
};

enum ConsoleColorBG
{
    BG_BLACK = 0,
    BG_DARKBLUE = BACKGROUND_BLUE,
    BG_DARKGREEN = BACKGROUND_GREEN,
    BG_DARKCYAN = BACKGROUND_GREEN | BACKGROUND_BLUE,
    BG_DARKRED = BACKGROUND_RED,
    BG_DARKMAGENTA = BACKGROUND_RED | BACKGROUND_BLUE,
    BG_DARKYELLOW = BACKGROUND_RED | BACKGROUND_GREEN,
    BG_DARKGRAY = BACKGROUND_RED | BACKGROUND_GREEN | BACKGROUND_BLUE,
    BG_GRAY = BACKGROUND_INTENSITY,
    BG_BLUE = BACKGROUND_INTENSITY | BACKGROUND_BLUE,
    BG_GREEN = BACKGROUND_INTENSITY | BACKGROUND_GREEN,
    BG_CYAN = BACKGROUND_INTENSITY | BACKGROUND_GREEN | BACKGROUND_BLUE,
    BG_RED = BACKGROUND_INTENSITY | BACKGROUND_RED,
    BG_MAGENTA = BACKGROUND_INTENSITY | BACKGROUND_RED | BACKGROUND_BLUE,
    BG_YELLOW = BACKGROUND_INTENSITY | BACKGROUND_RED | BACKGROUND_GREEN,
    BG_WHITE = BACKGROUND_INTENSITY | BACKGROUND_RED | BACKGROUND_GREEN | BACKGROUND_BLUE,
};

class WinConcole
{
public:
    static void Print(const std::wstring& str, WORD wColorFlags = BG_BLACK | FG_WHITE);
    static void PrintLn(const std::wstring& str, WORD color = BG_BLACK | FG_WHITE);
};

// TODO: finish making this into a neat singleton
class ImplLog
{
public:
    static void LogActionInfo(const std::string& _strMsg);
    static void LogSimpleWithColor(const std::string& _strMsg, WORD wColorFlags = BG_BLACK | FG_WHITE);
    static void LogAction_success(const std::string& _strMsg);
    static bool LogActionErrorFalse(const std::string& _strMsg);
    static bool LogAction_warning(const std::string& _strMsg);
    static void LogWrite(const std::string& _strMsg);
    static void WriteToLogFile(const std::string& logString);

    static void LogActionTimedBegin(const std::string& _strMsg);
    static void LogActionTimedEnd(const std::string& _strMsg);

    
    static ImplLog& GetInstance() { 
        if (!m_poInstance) 
        {
            m_poInstance = std::make_unique<ImplLog>();
        }
        else 
        {
            return *m_poInstance;
        }
    };   

    static tools::SystemClock m_globalClock;

private:
    static std::unique_ptr<ImplLog> m_poInstance;
};

class TimeLogAction
{
public:
    static TimeLogAction MakeObject() { return TimeLogAction(); };


    TimeLogAction() { m_clock.ResetLocalTime(); };

    static TimeLogAction PrintStart(const std::string& _strMsg)
    {         
        TimeLogAction newInstance;
        newInstance.m_message = _strMsg + "...";
        ImplLog::LogSimpleWithColor(prefix + newInstance.m_message, BG_BLACK | FG_WHITE);
        
        return newInstance;
    };

    void PrintDone(const std::string& _strMsg = "")
    {
        auto messageToShow = _strMsg.empty() ? m_message + ": Done." : _strMsg;
        ImplLog::LogSimpleWithColor(prefix +  messageToShow, BG_BLACK | FG_WHITE);
        auto timeElapsedMessageString = "Time Elapsed: " + std::to_string(m_clock.GetLocalTime()) + " seconds\n";

        ImplLog::LogSimpleWithColor(timeElapsedMessageString, BG_BLACK | FG_GREEN);
    };

    tools::SystemClock & GetClock() { return m_clock; };
    double GetLocalTime() { return m_clock.GetLocalTime(); };

private:
    tools::SystemClock m_clock;
    static constexpr char prefix[] = "[FBX SDK wrapper dll:] ";
    std::string m_message = "";
};



//namespace ConsoleForeground
//{
//    enum {
//        BLACK = 0,
//        DARKBLUE = FOREGROUND_BLUE,
//        DARKGREEN = FOREGROUND_GREEN,
//        DARKCYAN = FOREGROUND_GREEN | FOREGROUND_BLUE,
//        DARKRED = FOREGROUND_RED,
//        DARKMAGENTA = FOREGROUND_RED | FOREGROUND_BLUE,
//        DARKYELLOW = FOREGROUND_RED | FOREGROUND_GREEN,
//        DARKGRAY = FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE,
//        GRAY = FOREGROUND_INTENSITY,
//        BLUE = FOREGROUND_INTENSITY | FOREGROUND_BLUE,
//        GREEN = FOREGROUND_INTENSITY | FOREGROUND_GREEN,
//        CYAN = FOREGROUND_INTENSITY | FOREGROUND_GREEN | FOREGROUND_BLUE,
//        RED = FOREGROUND_INTENSITY | FOREGROUND_RED,
//        MAGENTA = FOREGROUND_INTENSITY | FOREGROUND_RED | FOREGROUND_BLUE,
//        YELLOW = FOREGROUND_INTENSITY | FOREGROUND_RED | FOREGROUND_GREEN,
//        WHITE = FOREGROUND_INTENSITY | FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE,
//    };
//}
//
//namespace ConsoleBackground
//{
//    enum ConsoleBackground {
//        BLACK = 0,
//        DARKBLUE = BACKGROUND_BLUE,
//        DARKGREEN = BACKGROUND_GREEN,
//        DARKCYAN = BACKGROUND_GREEN | BACKGROUND_BLUE,
//        DARKRED = BACKGROUND_RED,
//        DARKMAGENTA = BACKGROUND_RED | BACKGROUND_BLUE,
//        DARKYELLOW = BACKGROUND_RED | BACKGROUND_GREEN,
//        DARKGRAY = BACKGROUND_RED | BACKGROUND_GREEN | BACKGROUND_BLUE,
//        GRAY = BACKGROUND_INTENSITY,
//        BLUE = BACKGROUND_INTENSITY | BACKGROUND_BLUE,
//        GREEN = BACKGROUND_INTENSITY | BACKGROUND_GREEN,
//        CYAN = BACKGROUND_INTENSITY | BACKGROUND_GREEN | BACKGROUND_BLUE,
//        RED = BACKGROUND_INTENSITY | BACKGROUND_RED,
//        MAGENTA = BACKGROUND_INTENSITY | BACKGROUND_RED | BACKGROUND_BLUE,
//        YELLOW = BACKGROUND_INTENSITY | BACKGROUND_RED | BACKGROUND_GREEN,
//        WHITE = BACKGROUND_INTENSITY | BACKGROUND_RED | BACKGROUND_GREEN | BACKGROUND_BLUE,
//    };
//};


