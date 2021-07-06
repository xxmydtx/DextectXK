using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Odbc;


namespace Detect_xk
{
    class DataBase
    {
        public static OdbcConnection odbcCon;
        public static void connectToDataBase(ref DataGridView dataGridView,ref Button btn_connect,ref Button btn_query_all,ref Button btn_query_range)
        {
            if(btn_connect.Text == "连接数据库")
            {
                try
                {
                    //DSN:mylink数据源的名称 UID:sql server登录时的身份sa PWD:登录时的密码123456
                    //生成连接数据库字符串
                    string ConStr = "DSN=local_database32;UID=sa;PWD=123456;database=SQLTest;";
                    //定义SqlConnection对象实例
                    odbcCon = new OdbcConnection(ConStr);
                    btn_connect.Text = "已连接";
                    btn_query_all.Enabled = true;
                    btn_query_range.Enabled = true ;
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                odbcCon.Close();
                btn_connect.Text = "连接数据库";
                btn_query_all.Enabled = false;
                btn_query_range.Enabled = false;

            }
        }
        public static void GetAllData(ref DataGridView dataGridView)
        {
            string SqlStr = "select * from student";
            OdbcDataAdapter odbcAdapter = new OdbcDataAdapter(SqlStr, odbcCon);
            DataSet ds = new DataSet();
            odbcAdapter.Fill(ds);
            dataGridView.DataSource = ds.Tables[0].DefaultView;
        }
        public static void GetRangeData(ref DataGridView dataGridView, DateTime beg, DateTime end)
        {
            string YearBeg = beg.Year.ToString();
            string YearEnd = end.Year.ToString();
            string MonthBeg = beg.Month.ToString();
            string MonthEnd = end.Month.ToString();
            string DayBeg = beg.Day.ToString();
            string DayEnd = end.Day.ToString();
            string date_beg = YearBeg + "-" + MonthBeg + "-" + DayBeg;
            string date_end = YearEnd + "-" + MonthEnd + "-" + DayEnd;
            string SqlStr = "select * from student where 日期 between '" + date_beg + "' And '"+ date_end+"'";
            OdbcDataAdapter odbcAdapter = new OdbcDataAdapter(SqlStr, odbcCon);
            DataSet ds = new DataSet();
            odbcAdapter.Fill(ds);
            dataGridView.DataSource = ds.Tables[0].DefaultView;
        }
    }
}
