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
        public static void connectToDataBase(ref DataGridView dataGridView)
        {
            try
            {
                //DSN:mylink数据源的名称 UID:sql server登录时的身份sa PWD:登录时的密码123456
                //生成连接数据库字符串
                string ConStr = "DSN=local_database32;UID=sa;PWD=123456;database=SQLTest;";
                //定义SqlConnection对象实例
                OdbcConnection odbcCon = new OdbcConnection(ConStr);
                string SqlStr = "select * from student";

                OdbcDataAdapter odbcAdapter = new OdbcDataAdapter(SqlStr, odbcCon);
                DataSet ds = new DataSet();

                odbcAdapter.Fill(ds);
                dataGridView.DataSource = ds.Tables[0].DefaultView;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        
    }
}
