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
    /// http://awsdocs.s3.amazonaws.com/cloudsearch/2011-02-01/cloudsearch-dg-2011-02-01.pdf
    /// https://www.reddit.com/search?q=%28and+subreddit:%27SeattleWA%27+flair_text:%27government%27+ups:5..%29&sort=top&syntax=cloudsearch
    /// https://www.reddit.com/search?q=%28and+subreddit%3A%27seattle%27+%28not+is_self%3A1%29+ups%3A5..+ups%3A..20%29&sort=top&syntax=cloudsearch
    /// https://www.reddit.com/r/MusicGuides/search?rank=title&q=ups%3A{1,3}+is_self%3A1&restrict_sr=on&syntax=cloudsearch
    /// https://www.reddit.com/r/MusicGuides/search?rank=title&q=is_self%3A1&restrict_sr=on&syntax=cloudsearch
    /// 
    /// NOTE int ranges are <intfIeld>:<lowerbound>..<upperbound> not the bracket syntax that AWS docs list
    /// </summary>
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
                string op = expression.ToOperator(false);

                result = string.Format(op, FilterCallbinder(expression.Operand));
            }
            return result;
        }

        public static string FilterHelper(ConditionalExpression expression)
        {
            return expression.InvokeGet().ToString(); 
        }

        private static readonly List<ExpressionType> conditionalTypes = new List<ExpressionType>()
        {
            ExpressionType.AndAlso,
            ExpressionType.And,
            ExpressionType.OrElse,
            ExpressionType.Or
        };

        private static readonly List<ExpressionType> evaluateExpressions = new List<ExpressionType>()
        {
            ExpressionType.Add,
            ExpressionType.Subtract,
            ExpressionType.Multiply,
            ExpressionType.Divide,
            ExpressionType.Coalesce,
            ExpressionType.Conditional
        };

        public static string FilterHelper(BinaryExpression expression)
        {
            string op = null;
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
            else if (evaluateExpressions.Any(x=> x == expression.NodeType))
            {
                op = expression.ToOperator(false);
                return expression.InvokeGet().ToString();
            }
            else if (expression.NodeType == ExpressionType.NotEqual)
            {
                //NotEqualHandler();
            }
            bool isCorrectOrder = IsCorrectOrder(expression);
            op = op ?? expression.ToOperator(isCorrectOrder);
            string left = l ?? FilterCallbinder(expression.Left);
            string right = r ?? FilterCallbinder(expression.Right);

            left = AdjustForRange(expression.NodeType, left, true);
            right = AdjustForRange(expression.NodeType, right, false);

            if (!isCorrectOrder)
            {
                string temp = null;
                temp = left;
                left = right;
                right = temp;
            }

            return string.Format(op, left, right);
            
        }

        private static bool IsCorrectOrder(BinaryExpression expression)
        {
            MemberExpression member = expression.Left as MemberExpression;
            bool leftIsSearchProperty = member != null && (member.Member.DeclaringType == typeof(CloudSearchFilter));
            bool rightIsSearchProperty = false;
            if(!leftIsSearchProperty)
            {
                member = expression.Right as MemberExpression;
                rightIsSearchProperty = member != null && (member.Member.DeclaringType == typeof(CloudSearchFilter));
            }
            return leftIsSearchProperty || (!leftIsSearchProperty && !rightIsSearchProperty);

        }

        private static string AdjustForRange(ExpressionType expressionType, string element, bool isLeft)
        {
            string result = element;
            int output = 0;
            if (
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
    public static class Extensions
    {
        public static string ToOperator(this Expression expression, bool isCorrectOrder)
        {
            ExpressionType? type = expression?.NodeType;
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
                case ExpressionType.Coalesce:
                case ExpressionType.Equal:
                    result = "{0}:{1}";
                    break;
                case ExpressionType.ExclusiveOr:
                    break;
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    if (isCorrectOrder)
                    { result = "{0}:{1}.."; }
                    else
                    { result = "{0}:..{1}"; }
                    break;
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    if (isCorrectOrder)
                    { result = "{0}:..{1}"; }
                    else
                    { result = "{0}:{1}.."; }

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
                    BinaryExpression ex = expression as BinaryExpression;

                    if( ex.Left.Type == typeof(string) || ex.Right.Type == typeof(string) )
                    {
                        result = "{0}:'-{1}'";
                    }
                    else
                    {
                        result = "(NOT+{0}:{1})";
                    }
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

        
        public static object InvokeGet(this MemberExpression member)
        {
            return InvokeGetExpression(member);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        /// <remarks>
        /// source : http://stackoverflow.com/a/2616980
        /// </remarks>
        private static object InvokeGetExpression(Expression expression)
        {
            var objectMember = Expression.Convert(expression, typeof(object));

            var getterLambda = Expression.Lambda<Func<object>>(objectMember);

            var getter = getterLambda.Compile();

            return getter();
        }

        public static object InvokeGet(this MethodCallExpression call)
        {
            return InvokeGetExpression(call);
        }

        public static object InvokeGet(this BinaryExpression expression)
        {
            return InvokeGetExpression(expression);
        }

        public static object InvokeGet(this ConditionalExpression expression)
        {
            return InvokeGetExpression(expression);
        }
    }
}
