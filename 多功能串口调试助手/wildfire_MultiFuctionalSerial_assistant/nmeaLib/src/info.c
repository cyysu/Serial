/*
 *
 * NMEA library
 * URL: http://nmea.sourceforge.net
 * Author: Tim (xtimor@gmail.com)
 * Licence: http://www.gnu.org/licenses/lgpl.html
 * $Id: info.c 17 2008-03-11 11:56:11Z xtimor $
 *
 */

#include <string.h>

#include "nmea/info.h"

#include "nmea/context.h"
#include <stdio.h>
#include <io.h>

void trace(const char *str, int str_size)
{
	printf("Trace: ");
	write(1, str, str_size);
	printf("\n");
}
void error(const char *str, int str_size)
{
	printf("Error: ");
	write(1, str, str_size);
	printf("\n");
}

void nmea_zero_INFO(nmeaINFO *info)
{
    memset(info, 0, sizeof(nmeaINFO));
    nmea_time_now(&info->utc);
    info->sig = NMEA_SIG_BAD;
    info->fix = NMEA_FIX_BAD;

	//nmea_property()->trace_func = &trace;
	//nmea_property()->error_func = &error;
}



/********************************************************************************************************
**     函数名称:            bit        IsLeapYear(uint8_t    iYear)
**    功能描述:            判断闰年(仅针对于2000以后的年份)
**    入口参数：            iYear    两位年数
**    出口参数:            uint8_t        1:为闰年    0:为平年
********************************************************************************************************/
static int IsLeapYear(int iYear)
{
	int    Year;
	Year = 2000 + iYear;
	if ((Year & 3) == 0)
	{
		return ((Year % 400 == 0) || (Year % 100 != 0));
	}
	return 0;
}

/********************************************************************************************************
**     函数名称:            void    GMTconvert(uint8_t *DT,uint8_t GMT,uint8_t AREA)
**    功能描述:            格林尼治时间换算世界各时区时间
**    入口参数：            *DT:    表示日期时间的数组 格式 YY,MM,DD,HH,MM,SS
**                        GMT:    时区数
**                        AREA:    1(+)东区 W0(-)西区
********************************************************************************************************/
void    GMTconvert(nmeaTIME *SourceTime, nmeaTIME *ConvertTime, int GMT, int AREA)
{
	int    YY, MM, DD, hh, mm, ss;        //年月日时分秒暂存变量 

	if (GMT == 0)    return;                //如果处于0时区直接返回 
	if (GMT>12)    return;                //时区最大为12 超过则返回         

	YY = SourceTime->year;                //获取年 
	MM = SourceTime->mon;                 //获取月 
	DD = SourceTime->day;                 //获取日 
	hh = SourceTime->hour;                //获取时 
	mm = SourceTime->min;                 //获取分 
	ss = SourceTime->sec;                 //获取秒 

	if (AREA)                        //东(+)时区处理 
	{
		if (hh + GMT<24)    hh += GMT;//如果与格林尼治时间处于同一天则仅加小时即可 
		else                        //如果已经晚于格林尼治时间1天则进行日期处理 
		{
			hh = hh + GMT - 24;        //先得出时间 
			if (MM == 1 || MM == 3 || MM == 5 || MM == 7 || MM == 8 || MM == 10)    //大月份(12月单独处理) 
			{
				if (DD<31)    DD++;
				else
				{
					DD = 1;
					MM++;
				}
			}
			else if (MM == 4 || MM == 6 || MM == 9 || MM == 11)                //小月份2月单独处理) 
			{
				if (DD<30)    DD++;
				else
				{
					DD = 1;
					MM++;
				}
			}
			else if (MM == 2)    //处理2月份 
			{
				if ((DD == 29) || (DD == 28 && IsLeapYear(YY) == 0))        //本来是闰年且是2月29日 或者不是闰年且是2月28日 
				{
					DD = 1;
					MM++;
				}
				else    DD++;
			}
			else if (MM == 12)    //处理12月份 
			{
				if (DD<31)    DD++;
				else        //跨年最后一天 
				{
					DD = 1;
					MM = 1;
					YY++;
				}
			}
		}
	}
	else
	{
		if (hh >= GMT)    hh -= GMT;    //如果与格林尼治时间处于同一天则仅减小时即可 
		else                        //如果已经早于格林尼治时间1天则进行日期处理 
		{
			hh = hh + 24 - GMT;        //先得出时间 
			if (MM == 2 || MM == 4 || MM == 6 || MM == 8 || MM == 9 || MM == 11)    //上月是大月份(1月单独处理) 
			{
				if (DD>1)    DD--;
				else
				{
					DD = 31;
					MM--;
				}
			}
			else if (MM == 5 || MM == 7 || MM == 10 || MM == 12)                //上月是小月份2月单独处理) 
			{
				if (DD>1)    DD--;
				else
				{
					DD = 30;
					MM--;
				}
			}
			else if (MM == 3)    //处理上个月是2月份 
			{
				if ((DD == 1) && IsLeapYear(YY) == 0)                    //不是闰年 
				{
					DD = 28;
					MM--;
				}
				else    DD--;
			}
			else if (MM == 1)    //处理1月份 
			{
				if (DD>1)    DD--;
				else        //新年第一天 
				{
					DD = 31;
					MM = 12;
					YY--;
				}
			}
		}
	}

	ConvertTime->year = YY+1900;           //更新年 
	ConvertTime->mon = MM+1;                //更新月 
	ConvertTime->day = DD;                //更新日 
	ConvertTime->hour = hh;                //更新时 
	ConvertTime->min = mm;                //更新分 
	ConvertTime->sec = ss;                //更新秒 
}

/*经纬度转换 ： 输入nmea的 度分.xx 格式，输出 度.xxx */
double DegreeConvert(double sDegree)
{
	double dDegree;

	if (sDegree == 0)return 0;

	int integer = (int)sDegree;
	double decimal = sDegree - (int)sDegree;

	double min = integer % 100;
	int hour =(int) (integer / 100);

	dDegree = (double)hour + (double)(min / 60) + (double)(decimal / 60);

	

	return dDegree;
	
}

