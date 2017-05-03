using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using RedditSharp.Things;
namespace RedditSharp.Search
{
    /// <summary>
    /// https://github.com/reddit/reddit/blob/master/r2/r2/lib/providers/search/common.py
    /// </summary>
    public interface ICloudSearchFilter
    {
        CompoundSearchOperator GetCompoundSearchOperator();

        ICloudSearchFilter AddAndFilter(ICloudSearchFilter filter);
        ICloudSearchFilter AddOrFilter(ICloudSearchFilter filter);

        ICloudSearchFilter AddUtcDateRange(DateTime from, DateTime to);
        ICloudSearchFilter AddUtcDateRanges(IEnumerable<CloudSearchFilter.UtcDateRange> utcDateRanges);

        ICloudSearchFilter Over18(bool over18);
        //ICloudSearchFilter
    }

    public class CloudSearchFilter
    {
        public string title;
        public string subreddit;
        public string flair_text;
        
        public int ups;
        public bool over18;

        

        public bool timestamp(DateTime? from, DateTime? end)
        {
            return true;
        }
        

        public class UtcDateRange
        {
            private UtcDateRange() { }
            //public DateTime From { get; set; }
            //public DateTime To { get; set; }
            public bool Between(DateTime? from, DateTime? end)
            {
                return true;
            }
            //public static bool operator ==(DateTime filter, UtcDateRange dateRange)
            //{
            //    return true;
            //}
            //public static bool operator !=(DateTime filter, UtcDateRange dateRange)
            //{
            //    return true;
            //}
        }
        public static string Filter(Expression<Func<CloudSearchFilter, bool>> expression)
        {
            var x = FilterCallbinder(expression.Body);

            return x;
        }
        public static string Filter(Expression<Func<CloudSearchFilter, bool>> expression, Expression<Func<CloudSearchFilter, bool>> expression2, Expression<Func<CloudSearchFilter, bool>> expression3, Expression<Func<CloudSearchFilter, bool>> expression4)
        {
            var x = FilterCallbinder(expression4.Body);

            return x;
        }
        
        
        public static string FilterHelper(UnaryExpression expression)
        {
            //if(expression.NodeType != ExpressionType.Not)
            //{ throw new NotSupportedException("Only supports UnaryExpression for 'Not'"); }

            string result = string.Empty;

            if(expression.Operand.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression member = expression.Operand as MemberExpression;
                result = GetMemberFilter(member, false);
            }
            else
            {
                string op = expression.NodeType.ToOperator();

                result = string.Format(op, FilterCallbinder(expression.Operand));
            }
            return result;


        }

        private static readonly List<ExpressionType> conditionalTypes = new List<ExpressionType>()
        {
            ExpressionType.AndAlso,
            ExpressionType.And,
            ExpressionType.OrElse,
            ExpressionType.Or
        };



        public static string FilterHelper(BinaryExpression expression)
        {
            string op = expression.NodeType.ToOperator();
            string l = null;
            string r = null;
            
            if (conditionalTypes.Any(x => x == expression.NodeType))
            {

                if (expression.NodeType == expression.Left.NodeType)
                {
                    BinaryExpression leftExpression = expression.Left as BinaryExpression;
                    l = FilterCallbinder(leftExpression.Left) + "+" + FilterCallbinder(leftExpression.Right);
                }

                if (expression.NodeType == expression.Right.NodeType)
                {
                    BinaryExpression rightExpression = expression.Left as BinaryExpression;
                    r = FilterCallbinder(rightExpression.Left) + "+" + FilterCallbinder(rightExpression.Right);
                }
            }
            else if (expression.NodeType == ExpressionType.NotEqual)
            {
                //NotEqualHandler();
            }


            string left = l ?? FilterCallbinder(expression.Left);
            string right = r ?? FilterCallbinder(expression.Right);

            left = AdjustForRange(expression.NodeType, left, true);
            right = AdjustForRange(expression.NodeType, right, false);


            return string.Format(op, left, right);
            
        }


        private static string AdjustForRange(ExpressionType expressionType, string element, bool isLeft)
        {
            string result = element;
            int output = 0;
            if ((expressionType == ExpressionType.GreaterThan || expressionType == ExpressionType.NotEqual) &&
                int.TryParse(element, out output))
            {
                if(isLeft && expressionType == ExpressionType.GreaterThan)
                {
                    result = (output - 1).ToString();
                }
                else if (isLeft && expressionType == ExpressionType.LessThan)
                {
                    result = (output + 1).ToString();
                }
                else if (!isLeft && expressionType == ExpressionType.GreaterThan)
                {
                    result = (output + 1).ToString();
                }
                else if (!isLeft && expressionType == ExpressionType.LessThan)
                {
                    result = (output - 1).ToString();
                }

            }
            return result;
        }


        private static string FilterHelper(MethodCallExpression call)
        {
            string from = null;
            string end = null;
            string result = string.Empty;
            if(call.Method.Name == nameof(CloudSearchFilter.timestamp))
            {
                from = FilterCallbinder(call.Arguments[0]);
                end = FilterCallbinder(call.Arguments[1]);
                

                result = $"timestamp:{from}..{end}";
            }
            else
            {
                result = call.InvokeGet().ToString();
            }

            return result;

        }
        
        public static string FilterHelper(MemberExpression member)
        {
            string result = string.Empty;
            result = GetMemberFilter(member,true);

            return result;
        }

        private static string GetMemberFilter(MemberExpression member, bool filterValue)
        {
            string result;
            
            if (member.Type == typeof(bool))
            {
                result = $"{member.Member.Name}:{(filterValue ? "1": "0")}";
            }
            
            else if (member.Member.DeclaringType != typeof(CloudSearchFilter))
            {
                object data = member.InvokeGet();

                if (data is DateTime)
                {
                    DateTimeOffset offset = new DateTimeOffset((DateTime)data);
                    result = offset.ToUnixTimeSeconds().ToString();
                }
                else
                {
                    result = data.ToString();
                }
                
            }
            //else if(member.Expression is ConstantExpression)
            //{
               
            //    //result = FilterHelper(member.Expression as ConstantExpression);
            //}
           
            else
            {
                result = member.Member.Name;
            }

            return result;
        }

        public static string FilterHelper(ConstantExpression constant)
        {

            return constant.Value.ToString();
        }

        // using dynamic to hack late binding so i don't have to 
        // write a bunch of "if is type then cast and call cases".
        private static string FilterCallbinder(dynamic expression)
        {
            return FilterHelper(expression);
        }

    }

    public enum CompoundSearchOperator
    {
        None,
        And,
        Or

    }
    public static class Extensions
    {
        public static string ToOperator(this ExpressionType type)
        {
            string result = string.Empty;
            switch (type)
            {
                case ExpressionType.And:
                    break;
                case ExpressionType.AndAlso:
                    result = "(and+{0}+{1})";
                    break;
                case ExpressionType.Conditional:
                    break;
                case ExpressionType.Equal:
                    result = "{0}:{1}";
                    break;
                case ExpressionType.ExclusiveOr:
                    break;
                case ExpressionType.GreaterThan:
                    result = "{0}:{1}.."; // extra processing needed to adjust
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    result = "{0}:{1}..";
                    break;
                case ExpressionType.LessThan:
                    result = "{0}:..{1}"; // extra processing needed to adjust
                    break;
                case ExpressionType.LessThanOrEqual:
                    result = "{0}:..{1}";  
                    break;
                case ExpressionType.MemberAccess:
                    break;
                case ExpressionType.Negate:
                    break;
                case ExpressionType.NegateChecked:
                    break;
                case ExpressionType.Not:
                    result = "(not+{0})";
                    break;
                case ExpressionType.NotEqual:
                    break;
                case ExpressionType.Or:
                    break;
                case ExpressionType.OrElse:
                    result = "(or+{0}+{1})";
                    break;
                case ExpressionType.IsTrue:
                    break;
                case ExpressionType.IsFalse:
                    break;
                default:
                    break;
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        /// <remarks>
        /// source : http://stackoverflow.com/a/2616980
        /// </remarks>
        public static object InvokeGet(this MemberExpression member)
        {
            return InvokeGet(member as Expression);
        }

        private static object InvokeGet(Expression expression)
        {
            var objectMember = Expression.Convert(expression, typeof(object));

            var getterLambda = Expression.Lambda<Func<object>>(objectMember);

            var getter = getterLambda.Compile();

            return getter();
        }

        public static object InvokeGet(this MethodCallExpression call)
        {
            return InvokeGet(call as Expression);
        }
    }
}
