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
 
/// <summary>
/// using "std" as namespace to change some default behaviours
/// </summary>
namespace std
{    
    inline string to_string(bool _Val) {
        return (_Val ? "true" : "false");
    }
} 

namespace tools
{    
    static bool CompareCharsNoCase(char a, char b)
    {
        return std::tolower(static_cast<unsigned char>(a)) ==
            std::tolower(static_cast<unsigned char>(b));
    }

    static bool CompareStringNoCase(const std::string& a, const std::string& b)
    {
        return 
            a.size() == b.size() &&
            std::equal(a.begin(), a.end(), b.begin(), CompareCharsNoCase);
    }


	static std::string toLower(const std::string& _strInput)
	{
		std::string strOut(_strInput.size(), ' ');

		std::transform(_strInput.begin(), _strInput.end(), strOut.begin(), ::tolower);

		return strOut;
	}

	static std::string toUpper(const std::string& _strInput)
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
			if (toLower(items[itemIndex]) == toLower(value))
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
            m_startTime = std::chrono::high_resolution_clock::now();
        }

		/// <summary>
		///  Get "local" time, 
		/// </summary>
		/// <returns>"state time" - "now" </returns>
		double GetLocalTime() 
		{            
            auto timeElapsed = std::chrono::high_resolution_clock::now();                     
            
            auto value = std::chrono::duration<float, std::chrono::seconds::period>(timeElapsed-m_startTime);
            
            float retValue = value.count();
                        
            return retValue;
		}

	private:
        std::chrono::steady_clock::time_point m_startTime;		
	};

}
