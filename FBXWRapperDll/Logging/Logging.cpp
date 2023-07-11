#include "Logging.h"
#include <fstream>
#include <fstream>
#include <iostream>
#include <sstream>
#include <iostream>

class WinConcole
{
public:
	static void Print(const std::wstring& str, WORD Color = ConsoleBackground::BLACK | ConsoleForeground::WHITE)
	{
		HANDLE h = GetStdHandle(STD_OUTPUT_HANDLE);		
		/*
		 * Set the new color information
		 */
		SetConsoleTextAttribute(h, Color);
		DWORD dwChars = 0;
		WriteConsole(h, str.data(), (DWORD)str.size(), &dwChars, NULL);
		
		/*
		* Set default color info
		*/
		SetConsoleTextAttribute(h, ConsoleBackground::BLACK | ConsoleForeground::WHITE);
		
	}

	static void PrintLn(const std::wstring& str, WORD color = ConsoleBackground::BLACK | ConsoleForeground::WHITE)
	{
		Print(str + L"\n", color);
	}
};

void logfunc::impl_log_action(const std::string& _strMsg)
{
	WinConcole::Print(L"FBX SDK ACTION:", ConsoleBackground::DARKBLUE | ConsoleForeground::WHITE);
	WinConcole::Print(L" ");
	WinConcole::Print(WidenStr(_strMsg));
	WinConcole::Print(L"\r\n");

	std::stringstream logString;
	logString << std::endl << "ACTION: " << (_strMsg).c_str();

	WriteToLogFile(logString.str());
}

void logfunc::LogInfo(const std::string& _strMsg)
{	
	WinConcole::Print(L"FBX SDK INFO:", ConsoleBackground::DARKCYAN | ConsoleForeground::WHITE);
	WinConcole::Print(L" ");
	WinConcole::Print(WidenStr(_strMsg));
	WinConcole::Print(L"\r\n");

	std::stringstream logString;
	logString << std::endl << "ACTION: " << (_strMsg).c_str();

	WriteToLogFile(logString.str());
}

void logfunc::impl_log_action_success(const std::string& _strMsg)
{	
	WinConcole::Print(L"FBX SDK ACTION: SUCCESS:", ConsoleBackground::DARKGREEN | ConsoleForeground::WHITE);
	WinConcole::Print(L" ");
	WinConcole::Print(WidenStr(_strMsg));
	//WinConcole::Print(L"Success.", ConsoleBackground::BLUE | ConsoleForeground::WHITE);
	WinConcole::Print(L"\r\n");

	std::stringstream logString;
	logString << std::endl << "FBX SDK ACTION: SUCCESS:" << _strMsg << ". Success.";
	
	WriteToLogFile(logString.str());	
}

bool logfunc::impllog_action_error(const std::string& _strMsg)
{	
	WinConcole::Print(L"FBX SDK ERROR:", ConsoleBackground::RED | ConsoleForeground::YELLOW);
	WinConcole::Print(L" ");
	WinConcole::Print(WidenStr(_strMsg));
	WinConcole::Print(L"\r\n");

	std::stringstream logString;
	logString << std::endl << "FBX SDK ERROR: " << _strMsg;

	WriteToLogFile(logString.str());

	return false;
}

bool logfunc::impl_log_action_warning(const std::string& _strMsg)
{
	WinConcole::Print(L"FBX SDK WARNING:", ConsoleBackground::MAGENTA | ConsoleForeground::WHITE);
	WinConcole::Print(L" ");
	WinConcole::Print(WidenStr(_strMsg));
	WinConcole::Print(L"\r\n");

	std::stringstream logString;
	logString << std::endl << "FBX SDK WARNING:: " << _strMsg;

	 WriteToLogFile(logString.str());

	return false;
}

bool logfunc::impl_log_action_warning(const std::wstring& _wstrMsg)
{
	return logfunc::impl_log_action_warning(NarrowStr(_wstrMsg));
}

void logfunc::impl_log_write(const std::string& _strMsg)
{
	WriteToLogFile(_strMsg);
}
