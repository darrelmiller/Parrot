using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninject;

namespace Parrot.Ninject
{
    public class NinjectParrotDependencyResolver : Infrastructure.DependencyResolver
    {
        private readonly IKernel _kernel;

        public NinjectParrotDependencyResolver(IKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException("kernel");
            }

            _kernel = kernel;
        }

        public override T Get<T>()
        {
            return _kernel.TryGet<T>() ?? base.Get<T>();
        }

        public override object Get(Type type)
        {
            return _kernel.TryGet(type) ?? base.Get(type);
        }
    }
}
