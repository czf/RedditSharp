using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RedditSharp.Things;
using RedditSharp;
using System.Net;
using UnitTesting.TestData;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Linq.Expressions;
using RedditSharp.Search;
namespace UnitTesting
{
    /// <summary>
    /// Summary description for CloudSearchTest
    /// </summary>
    [TestClass]
    public class CloudSearchTest
    {
        public CloudSearchTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }


        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void BoolPropertyTest()
        {
            //Arrange
            Expression<Func<CloudSearchFilter, bool>> 
                expression = x => !x.over18;
            string expected = "over18:0";
            

            //Act
            string actual = CloudSearchFilter.Filter(expression);

            //Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void PropertyGreaterThanVariable()
        {
            //Arrange
            int minUps = IntVal();

            Expression<Func<CloudSearchFilter, bool>>
                expression = x => !(x.ups > minUps);
            string expected = $"(not+ups:{minUps+1}..)";

            //Act
            string actual = CloudSearchFilter.Filter(expression);

            //Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void PropertyGreaterThanOrEqualObjectMethod()
        {
            //Arrange
            testClass c = new testClass() ;

            Expression<Func<CloudSearchFilter, bool>>
                expression = x => !(x.ups >= c.method(5));
            string expected = $"(not+ups:{c.method(5)}..)";

            //Act
            string actual = CloudSearchFilter.Filter(expression);

            //Assert
            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void IntBetweenObjectFieldAndStaticMethod()
        {
            //Arrange
            testClass c = new testClass() { field = IntVal() * 100 };

            
            Expression<Func<CloudSearchFilter, bool>>
                expression = x => ( x.ups <= c.field &&  x.ups >= IntVal());

            //Would prefer the more condense output but wanted the more intutive 
            // comparision syntax to also be suppored.  One alternative is
            // to do something like the implementation for timestamp()
            //string expected = $"(ups:{minUps}..{maxUps})";
            string expected = $"(and+ups:..{c.field}+ups:{IntVal()}..)";
            

            //Act
            string actual = CloudSearchFilter.Filter(expression);

            //Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TimestampEndOnlyTest()
        {
            //Arrange
            DateTime end = new DateTime(2017, 3, 1);
            DateTimeOffset endOffset = new DateTimeOffset(end);
            Expression<Func<CloudSearchFilter, bool>>
                expression = x => x.timestamp(null, end);
            string expected = $"timestamp:..{endOffset.ToUnixTimeSeconds()}";


            //Act
            string actual = CloudSearchFilter.Filter(expression);

            //Assert
            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void TimestampStartOnlyTest()
        {
            //Arrange
            DateTime start = new DateTime(2017, 1, 1);
            DateTimeOffset startOffset = new DateTimeOffset(start);
            Expression<Func<CloudSearchFilter, bool>>
                expression = x => x.timestamp(start, null);
            string expected = $"timestamp:{startOffset.ToUnixTimeSeconds()}..";


            //Act
            string actual = CloudSearchFilter.Filter(expression);

            //Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TimestampRangeTest()
        {


            //Arrange
            DateTime start = new DateTime(2017, 1, 1);
            DateTime end = new DateTime(2017, 3, 1);
            DateTimeOffset endOffset = new DateTimeOffset(end);
            DateTimeOffset startOffset = new DateTimeOffset(start);
            Expression<Func<CloudSearchFilter, bool>>
                expression = x => x.timestamp(start, end);
            string expected = $"timestamp:{startOffset.ToUnixTimeSeconds()}..{endOffset.ToUnixTimeSeconds()}";


            //Act
            string actual = CloudSearchFilter.Filter(expression);

            //Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void OrTest()
        {
            //Arrange
            string name = CloudSearchTest.NameVal();
            Expression<Func<CloudSearchFilter, bool>>
                expression = x => x.subreddit == name || x.title == "'title'" || x.flair_text == "'flair text'";
            string expected = $"(or+subreddit:{name}+title:'title'+flair_text:'flair text')";


            //Act
            string actual = CloudSearchFilter.Filter(expression);

            //Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NotEqualStringTest()
        {
            //Arrange
            string name = CloudSearchTest.NameVal();
            Expression<Func<CloudSearchFilter, bool>>
                expression = x => x.subreddit != "'title'";
            string expected = $"(subreddit:'-title'";


            //Act
            string actual = CloudSearchFilter.Filter(expression);

            //Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NotEqualNumericTest()
        {
            //Arrange
            string name = CloudSearchTest.NameVal();
            Expression<Func<CloudSearchFilter, bool>>
                expression = x => x.ups != 5;
            string expected = $"(NOT ups:5..5)";


            //Act
            string actual = CloudSearchFilter.Filter(expression);

            //Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Or_AndTest()
        {
            //Arrange
            
            Expression<Func<CloudSearchFilter, bool>>
                expression = x => x.subreddit == CloudSearchTest.NameVal() || x.title == "'title'" && x.flair_text == "'flair text'";
            string expected = $"(or+subreddit:{CloudSearchTest.NameVal()}+(and+title:'title'+flair_text:'flair text'))";


            //Act
            string actual = CloudSearchFilter.Filter(expression);

            //Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CompoundConditionTest()
        {
            //Arrange
            string name = CloudSearchTest.NameVal();
            DateTime start = new DateTime(2017, 1, 1);
            DateTime end = new DateTime(2017, 3, 1);
            DateTimeOffset endOffset = new DateTimeOffset(end);
            DateTimeOffset startOffset = new DateTimeOffset(start);
            Expression<Func<CloudSearchFilter, bool>>
                expression = x => (x.timestamp(start, end) || x.subreddit == name || (x.title == "title" && ((x.flair_text == "govt" && x.ups > 4) || (x.ups > 5 && x.flair_text == "news"))));
            string expected = $"(or+timestamp:{startOffset.ToUnixTimeSeconds()}..{endOffset.ToUnixTimeSeconds()}+subreddit:{name}+(and+title:title+(or+(and+flair_text:govt+ups:5..)+(and+ups:6..+flair_text:news))))";
            //%28or+timestamp:1483257600..1488355200+subreddit:%27SeattleWA%27+%28and+title:%27title%27+%28or+%28and+flair_text:%27govt%27+ups:5..%29+%28and+ups:5..+flair_text:%27news%27%29%29%29%29
            //Act
            string actual = CloudSearchFilter.Filter(expression);

            //Assert
            Assert.AreEqual(expected, actual);
        }



        private static string NameVal() => "'SeattleWA'";
        
        private static int IntVal() => 4;
        
        private static DateTime? DatetimeVal() => new DateTime(2017, 1, 1);
    }

    public class testClass
    {
        public int field;
        public int method(int parameter)
        {
            return parameter;
        }
    }
}
