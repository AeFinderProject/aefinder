using System.Linq.Expressions;

namespace AElf.Indexing.Elasticsearch.Translator
{
    public class BinaryExpressionHandler
    {
        internal BinaryExpression Node { get; private set; }

        public Expression ValueExpression { get; set; }

        public MemberExpression PropertyExpression { get; set; }

        public bool IsRight { get; private set; }

        public bool HasPropertyExpression
        {
            get
            {
                return PropertyExpression != null;
            }
        }

        public BinaryExpressionHandler(BinaryExpression expression)
        {
            Node = expression;
            CheckProperty();
        }

        private void CheckProperty()
        {
            if (Node == null)
            {
                return;
            }

            if (Node.Left is MemberExpression leftMemberEx)
            {
                PropertyExpression = leftMemberEx;
                ValueExpression = Node.Right;
                return;
            }

            if (Node.Right is MemberExpression rightMemberEx)
            {
                PropertyExpression = rightMemberEx;
                ValueExpression = Node.Left;
                IsRight = true;
            }
        }
    }
}
