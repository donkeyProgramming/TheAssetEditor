/*
  file: tools.h
 
  Various tool functions
 
  Authored:
  phazer, 2020-2023 
 
 */

#pragma once

#include <vector>
#include <string>
#include <algorithm>
#include <chrono>
#include <codecvt>

/// <summary>
/// Overloads std::tolower()/toupper() to work for std::string (only works single-byte char in STL)
/// 
/// </summary>
namespace std
{
	static std::string tolower(const std::string& _strInput)
	{
		std::string strOut = _strInput;

		std::transform(strOut.begin(), strOut.end(), strOut.begin(), ::tolower);

		return strOut;
	}

	static std::string toupper(const std::string& _strInput)
	{
		std::string strOut = _strInput;

		std::transform(strOut.begin(), strOut.end(), strOut.begin(), ::tolower);

		return strOut;
	}
}

static std::wstring WidenStr(const std::string& str)
{
	using convert_typeX = std::codecvt_utf8<wchar_t>;
	std::wstring_convert<convert_typeX, wchar_t> converterX;

	return converterX.from_bytes(str);
}

/// <summary>
/// Various misc functions
/// </summary>
namespace tools
{
	static int GetIndexOfStringInVector(const std::string& value, const std::vector<std::string>& items)
	{
		for (int itemIndex = 0; itemIndex < items.size(); itemIndex++)
		{
			if (tolower(items[itemIndex]) == tolower(value))
				return itemIndex;
		}

		return -1;
	}

	/// <summary>
	/// Uses the CPUs high resolution clock, to count time intervals
	/// </summary>
	class SystemClock
	{
        typedef std::chrono::high_resolution_clock Time;        
	public:
        SystemClock()
        {
            ResetLocalTime(); 
        }		

        /// <summary>
        /// Resets the local timer to "now"
        /// </summary>
        void ResetLocalTime()
        {
            m_startTime = std::chrono::steady_clock::now();
        }

		/// <summary>
		///  Get "local" time, 
		/// </summary>
		/// <returns>"state time" - "now" </returns>
		double GetLocalTime() 
		{
            std::chrono::duration<double> timeElapsed = std::chrono::steady_clock::now() - m_startTime;                       

            double secondsElapsed =timeElapsed.count();
            return secondsElapsed;
		}

	private:
        std::chrono::time_point<std::chrono::steady_clock> m_startTime;		
	};

}
