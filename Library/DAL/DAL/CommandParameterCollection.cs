using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace DAL
{
    /// <summary>
    /// 参数集合
    /// </summary>
    public class CommandParameterCollection : IEnumerable
    {
        /// <summary>
        /// 容器，盛放参数
        /// </summary>
        private ArrayList AContainer = new ArrayList();

        /// <summary>
        /// 取得容器中参数的数量
        /// </summary>
        public int Count
        {
            get
            {
                return AContainer.Count;
            }
        }

        /// <summary>
        /// 添加元素
        /// </summary>
        /// <param name="mCommandParamenter">参数实体</param>
        /// <returns></returns>
        public CommandParameterCollection Add(CommandParameter mCommandParamenter)
        {
            this.AContainer.Add(mCommandParamenter);
            return this;
        }

        /// <summary>
        /// 添加元素
        /// </summary>
        /// <param name="mCommandParamenter">参数实体</param>
        /// <returns></returns>
        public CommandParameterCollection Add(string fieldName, object value)
        {
            this.AContainer.Add(new CommandParameter(fieldName, value));

            return this;
        }

        /// <summary>
        /// 清空容器
        /// </summary>
        /// <returns></returns>
        public CommandParameterCollection Clear()
        {
            this.AContainer.Clear();
            return this;
        }

        /// <summary>
        /// 取得参数实体
        /// </summary>
        /// <param name="iIndex"></param>
        /// <returns></returns>
        public CommandParameter this[int iIndex]
        {
            get
            {
                return (CommandParameter)this.AContainer[iIndex];
            }
        }

        #region 实现IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public IEnumerator GetEnumerator()
        {
            return new MyEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    /// <summary>
    /// Implement IEnumerator for CommandParameterCollection.
    /// </summary>
    public class MyEnumerator : IEnumerator
    {
        private int index;
        private CommandParameterCollection collection;

        /// <summary>
        /// 预设
        /// </summary>
        /// <param name="coll"></param>
        public MyEnumerator(CommandParameterCollection coll)
        {
            collection = coll;
            index = -1;
        }

        /// <summary>
        /// 重设索引
        /// </summary>
        public void Reset()
        {
            index = -1;
        }

        /// <summary>
        /// 移向下一个元素
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            index++;

            return (index < collection.Count);
        }

        /// <summary>
        /// 取得当前元素
        /// </summary>
        public CommandParameter Current
        {
            get { return (CommandParameter)collection[index]; }
        }

        /// <summary>
        /// 当前元素属性
        /// </summary>
        object IEnumerator.Current
        {
            get { return (Current); }
        }
    }

    /// <summary>
    /// 参数
    /// </summary>
    public class CommandParameter
    {
        private int _length;

        /// <summary>
        /// 栏位名称
        /// </summary>
        public string ColumnName { set; get; }

        /// <summary>
        /// 栏位属性
        /// </summary>
        public FieldType DBType { set; get; }

        private object _Value = null;

        /// <summary>
        /// 值
        /// </summary>
        public object Value
        {
            set
            {
                if (value == null)
                {
                    _Value = System.DBNull.Value;
                }
                else
                {
                    _Value = value;
                }
            }

            get { return _Value; }
        }

        //public SqlDbType Type { get; set; }

        /// <summary>
        /// 栏位长度
        /// </summary>
        public int Length
        {
            get
            {
                return this._length;
            }

            set
            {
                this._length = value;
            }
        }

        /// <summary>
        /// 初始化参数
        /// </summary>
        /// <param name="sColumnName">名称</param>
        /// <param name="value">值</param>
        public CommandParameter(string sColumnName, object value)
        {
            this.ColumnName = sColumnName;
            this.Value = value;
            this.DBType = FieldType.Object;
        }

        /// <summary>
        /// 初始化参数
        /// </summary>
        /// <param name="sColumnName">栏位名称</param>
        /// <param name="value">值</param>
        /// <param name="fFieldType">栏位数据类型</param>
        public CommandParameter(string sColumnName, object value, FieldType fFieldType)
        {
            this.ColumnName = sColumnName;
            this.Value = value;
            this.DBType = fFieldType;
        }

        /// <summary>
        /// 初始化参数
        /// </summary>
        /// <param name="sColumnName">栏位名称</param>
        /// <param name="value">值</param>
        /// <param name="fFieldType">栏位数据类型</param>
        public CommandParameter(string sColumnName, object value, FieldType dbType, int length)
        {
            this.ColumnName = sColumnName;
            this.Value = value;
            this.DBType = dbType;
            this.Length = length;
        }
    }

    /// <summary>
    /// 栏位数据类型
    /// </summary>
    public enum FieldType
    {
        DateTime,
        Decimal,
        Double,
        Int,
        Object,
        StringArray,
        String
    }
}
