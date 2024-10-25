using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.Core.VComponents
{
    public interface IGeneralInteractionModuleIntegrator
    {
        protected IGeneralInteractionModuleImplementor _implementor { get; } //Not visible to customer 

        public bool AdminOnly {
            get {
                return _implementor.AdminOnly;
            }
            set {
                _implementor.AdminOnly = value;
            }
        }
    }

    public interface IGeneralInteractionModuleImplementor
    {
        protected IGeneralInteractionModule _module { get; } //Not visible to customer 

        public bool AdminOnly
        {
            get
            {
                return _module.AdminOnly;
            }
            set
            {
                _module.AdminOnly = value;
            }
        }
    }

    public interface IGeneralInteractionModule
    {
        public bool AdminOnly { get; set; }
    }
}