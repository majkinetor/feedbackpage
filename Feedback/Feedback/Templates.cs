using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;


namespace Templates
{
    public class Template
    {        
        public class Field
        {
            public string name;             //name of the field            
            public string format="{0}";     //format
            public int position;            //position in template
            public int length;              //total length
        }
    
        protected Dictionary<string, object> UserFields = new Dictionary<string,object>(5);

        #region Event

        /// <summary>
        /// User can choose what to do on field error. Return null to stop parsing.
        /// Return empty string to ignore the field.
        /// Return custom string otherwise as value (or field.name to leave field in place (without delimiters))
        /// </summary>
        public event FieldErrorHandler FieldError;
        public delegate string FieldErrorHandler(object sender, Field field, Exception e);
        #endregion Event

         #region Properties
        public object this[string fieldName]
        {
            get { return UserFields[fieldName]; }
            set { UserFields[fieldName] = value; }
        }
        public bool ThrowExceptions = true;
        public string Layout
        {

            get { return layout; }
            set { layout = value; bShouldCompile = true; }
        }           //influences compiling
        public string NL = "\r\n";        //influences options
        public string Delimiters
        {
            get { return String.Format("{0} {1}", delim[0], delim[1]); }
            set
            {
                if ((value == null) || value.IndexOf(" ") == -1)
                    throw new ArgumentException("Delimiters must be in format \"startString endString\"", "Delimiters");
                delim = value.Split(' ');
                bShouldCompile = true;
            }
        }

        private List<Field> Fields = new List<Field>(10);
        private string[] delim = new string[2]{ "{", "}" } ;
        private string layout;               
        private bool bShouldCompile;
        private static MemberTypes memberTypes = MemberTypes.Field | MemberTypes.Property | MemberTypes.Method;
        private static BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                            BindingFlags.NonPublic | BindingFlags.ExactBinding | BindingFlags.FlattenHierarchy;

        #endregion Properties

        #region Constructors

        public Template(){}
        public Template(string layout)
        {
            Layout = layout;
        }

        #endregion Constructors

        public string Parse(object o) { return Parse(o, null); }
        public string Parse(object o, params TOption[] opts)
        {
                if (Layout == null) Layout = GetFieldValue(o, new Field { name = "_template" });
                if (Layout == String.Empty) return String.Empty;
                if (opts == null) opts = GetTOptions(o);

                if (bShouldCompile) Compile();

                string s;
                object value;
                int idx = 0;
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < Fields.Count; i++)
                {

                    sb.Append(s = layout.Substring(idx, Fields[i].position - idx));
                    value = GetFieldValue(o, Fields[i]);
                    sb.Append(value);

                    idx += Fields[i].length + s.Length;
                }
                sb.Append(layout.Substring(idx));

                return opts != null ? SetTOptions(sb, opts) : sb.ToString();
        }

        #region Private Methods

        protected string GetFieldValue(object o, Field f)
        {
            if (UserFields.ContainsKey(f.name)) return String.Format(f.format, UserFields[f.name]);
            
            int prevIdx = 0, dotIdx;
            bool nameHasDot;
            string name = default(string);
            Object value;    
            MemberInfo m;

            try
            {
                do
                {
                    dotIdx = f.name.IndexOf(".", prevIdx);
                    nameHasDot = dotIdx >= 0;

                    if (prevIdx >= 0)
                        name = nameHasDot ? f.name.Substring(prevIdx, dotIdx - prevIdx) : f.name.Substring(prevIdx);

                    if ((m = FindMember(o, name)) == null) throw new ArgumentException("Can't find member: " + f.name, "Template");

                    value = GetMemberValue(m, o, name);
                    if (IsTemplate(m)){
                        Template t = new Template { Delimiters = this.Delimiters, UserFields = this.UserFields };                       
                        object o1 = value;
                        if (value.GetType().Name == "String") { t.Layout = value as  string; o1 = o; }
                        value = t.Parse(o1, GetTOptions(m));
                    }

                    if (nameHasDot) { o = value; prevIdx = dotIdx + 1; }
                }
                while (nameHasDot);
            }
            catch (Exception e)
            {
                if (e.Message == "Exception has been thrown by the target of an invocation.") e = e.InnerException;
                
                string userVal=null;
                if (FieldError != null) userVal = FieldError(this, f, e);
                bool bThrow = (FieldError == null) ? ThrowExceptions : userVal == null;
                if (bThrow) throw new Exception(String.Format("Field at position {0} can not be found: {1}", f.position, f.name), e);
                return userVal;
            }

            return String.Format(f.format, value);
        }

        protected static MemberInfo FindMember(object o, string name)
        {
            if (name.StartsWith("!")) return GetIndexer(o, name.Substring(1));

            MemberInfo[] mi = o.GetType().GetMember(name, memberTypes, bindingFlags);
            if (mi.Length == 0)
                return GetIndexer(o, name);
            return mi[0];
        }

        private static MemberInfo GetIndexer(object o, string name)
        {
            MemberInfo[] mi = o.GetType().GetMember("Item", memberTypes, bindingFlags);

            object input = StringOrInteger(name);
            
            foreach (MemberInfo m in mi)
                if (m.ToString().Contains( input.GetType().Name + "]" )) return m;  //cute, eh ? Alternative is to be 20 times slower but semantically correct.

            return null;
        }    
    
        protected static void   ValidateFieldName(string name)
        {
            // ^[a-zA-Z_][a-zA-Z0-9_]*$
            string n = name.ToUpper();
            int j;
            for (int i = 0; i < n.Length; i++)
            {
                j = (int)n[i];
                if (!((j >= 65 && j <= 90) || (j >= 48 && j <= 57) || (j == 95 || j == 46)))
                    throw new ArgumentException("Field error: " + name, "Template");
            }

            //can't start with the number
            if (int.TryParse(n.Substring(0, 1), out j))
                throw new ArgumentException("Field error: " + name, "Template");
        }
        protected object GetMemberValue(MemberInfo mi, object o, string name)
        {
            switch (mi.MemberType)
            {
                case MemberTypes.Property: //if property doesn't exist try indexer
                    PropertyInfo pi = ((PropertyInfo)mi);
                    if (pi.GetGetMethod().GetParameters().Length == 1)
                    {
                        if (name.StartsWith("!")) name = name.Substring(1);
                        return pi.GetValue(o, new object[] { StringOrInteger(name) });
                    }
                    else return pi.GetValue(o, null);
                break;
                case MemberTypes.Field:  return ((FieldInfo)mi).GetValue(o);
                case MemberTypes.Method: return ((MethodInfo)mi).Invoke(o, null);
                default: return null;
            }            
        }

        private static object StringOrInteger(string name)
        {   
            int result;
            if (int.TryParse(name, out result))
                 return result;
            else return name;
        }
        protected bool IsTemplate(MemberInfo mi)
        {
            return mi.IsDefined(typeof(TemplateAttribute), false);
        }
        protected TOption[] GetTOptions(object o)
        {
            Type t = o.GetType();
            
            if (t.IsDefined(typeof(TemplateAttribute), false))
                return ((TemplateAttribute)t.GetCustomAttributes(typeof(TemplateAttribute), false)[0]).Options;
            
            return null;
        }
        protected TOption[] GetTOptions(MemberInfo mi)
        {
            return ((TemplateAttribute)(mi.GetCustomAttributes(typeof(TemplateAttribute), false)[0])).Options;
        }
        protected string SetTOptions(StringBuilder sb, TOption[] opts)
        {
            bool delLastLine = false;
            foreach (TOption to in opts)
            {
                if (to == TOption.Join) { sb = sb.Replace(NL, String.Empty); continue; }

                string[] lines = sb.ToString().Split(new string[] { NL }, StringSplitOptions.None);
                sb = new System.Text.StringBuilder();
                switch (to)
                {
                    case TOption.TrimColumn:
                        int trimColumn = -1;
                        foreach (string line in lines)
                        {
                            if (line == String.Empty) continue;

                            int i = 0;
                            while (i < line.Length)
                            {
                                if (line[i] == ' ' || line[i] == '\t') i++; else break;
                                if (trimColumn != -1 && i >= trimColumn) break;
                            }
                            if (trimColumn == -1 || i < trimColumn) trimColumn = i;
                            if (trimColumn == 1) break;
                        }

                        foreach (string line in lines)
                            sb.Append(trimColumn < line.Length ? line.Substring(trimColumn) + NL : NL);

                        delLastLine = true;
                        break;

                    case TOption.Trim:
                    case TOption.TrimStart:
                    case TOption.TrimEnd:
                        foreach (string line in lines)
                            if (to == TOption.Trim)
                                 sb.Append(line.Trim() + NL);
                            else sb.Append(to == TOption.TrimStart ? line.TrimStart() + NL : line.TrimEnd() + NL);
                        delLastLine = true;
                        break;
                   
                    case TOption.DelBlanks:
                        for (int i = 0; i < lines.Length-1; i++)
                           if (lines[i].Trim() == String.Empty) continue; else sb.Append(lines[i] + NL);
                        if (lines[lines.Length-1].Trim() == String.Empty) continue; else sb.Append(lines[lines.Length-1]);
                        break;

                    case TOption.JoinNonBlanks:
                        foreach (string line in lines)
                            if (line.Trim() == String.Empty) continue; else sb.Append(line);
                        break;

                    case TOption.Compact:
                        foreach (string line in lines)
                            if (line.Trim() == String.Empty) continue; else sb.Append(line.Trim());
                        break;
                }

                if (delLastLine) { delLastLine = false; sb.Remove(sb.Length - NL.Length, NL.Length); }
            }
            return sb.ToString();
        }
        protected void Compile()
        {
            int j, idx, startIndex = 0;
            int delimLength = delim[0].Length + delim[1].Length;

            Field field;
            Fields.Clear();
            while (true)
            {
                startIndex = layout.IndexOf(delim[0], startIndex);
                if (startIndex == -1) break;
                idx = layout.IndexOf(delim[1], startIndex);
                if (idx == -1) throw new ArgumentException("Field error:\n\n" + layout.Substring(startIndex, 10), "Template");

                field = new Field();
                field.position = startIndex;
                field.name = layout.Substring(startIndex + delim[0].Length, idx - startIndex - delim[1].Length);
                field.length = field.name.Length + delimLength;

                j = field.name.IndexOfAny(new char[] { ',', ':' });
                if (j != -1)
                {
                    field.format = "{0" + field.name.Substring(j) + "}";
                    field.name = field.name.Substring(0, j);
                } 

                //ValidateFieldName(field.name);

                Fields.Add(field);
                startIndex = idx + delim[1].Length;
            }

            bShouldCompile = false;
        }

        #endregion Private Methods

    }

    public class TemplateAttribute : Attribute
    {
        internal TOption[] Options;
        public TemplateAttribute()
        {
        }
        public TemplateAttribute(params TOption[] options)
        {
            Options = options;
        }
    }

    public enum TOption { TrimStart, TrimColumn, TrimEnd, Trim, DelBlanks, Join, JoinNonBlanks, Compact };
}