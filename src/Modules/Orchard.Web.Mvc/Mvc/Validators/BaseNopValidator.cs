using FluentValidation;

namespace Orchard.Web.Mvc.Validators
{

    /// <summary>
    /// ��֤��
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public  class BaseNopValidator<T> : AbstractValidator<T> where T : class
    {
        /// <summary>
        /// ��֤�๹�췽�� 
        /// </summary>
        protected BaseNopValidator()
        {
            PostInitialize();
        }

        /// <summary>
        /// Developers can override this method in custom partial classes
        /// in order to add some custom initialization code to constructors
        /// </summary>
        protected virtual void PostInitialize()
        {
            
        }
    }
}