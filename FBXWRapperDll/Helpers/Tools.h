/*
 * tools.h
 *
 * Various tool functions
 *
 * phazer, 2023 
 *
 */
#pragma once

#include <vector>
#include <string>
#include <algorithm>
#include <chrono>
#include <codecvt>

/// <summary>
/// Overloads std::tolower()/toupper() to std::string (only works single char in STL)
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
	


	/*template <typename T>
	static int GetIndexOf(const T& value, const std::vector<T>& items)
	{
		for (int itemIndex = 0; itemIndex < items.size(); itemIndex++)
		{
			if (items[itemIndex] == value)
				return itemIndex;
		}
	}

	template<>*/
	static int GetIndexOf(const std::string& value, const std::vector<std::string>& items)
	{
		for (int itemIndex = 0; itemIndex < items.size(); itemIndex++)
		{
			if (tolower(items[itemIndex]) == tolower(value))
				return itemIndex;
		}

		return -1;
	}

	class SystemClock
	{
        typedef std::chrono::high_resolution_clock Time;        
	public:
        SystemClock()
        {
            ResetLocalTime();
        }

		//double GetSeconds()
  //      {				
		//	//auto ticks = std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::steady_clock::now());

		//	/*double ticksPerSeconds = std::chrono::high_resolution_clock::period::den;

		//	double seconds = ticks / ticksPerSeconds;			*/

		//	return seconds;			
		//}

        void ResetLocalTime()
        {
            m_startTime = std::chrono::steady_clock::now();
        }

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
