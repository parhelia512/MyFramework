using System;
using ILRuntime.CLR.Method;
using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;
#if DEBUG && !DISABLE_ILRUNTIME_DEBUG
using AutoList = System.Collections.Generic.List<object>;
#else
using AutoList = ILRuntime.Other.UncheckedList<object>;
#endif

namespace HotFix
{   
    public class WindowObjectAdapter : CrossBindingAdaptor
    {
        public override Type BaseCLRType
        {
            get
            {
                return typeof(global::WindowObject);
            }
        }

        public override Type AdaptorType
        {
            get
            {
                return typeof(Adapter);
            }
        }

        public override object CreateCLRInstance(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
        {
            return new Adapter(appdomain, instance);
        }

        public class Adapter : global::WindowObject, CrossBindingAdaptorType
        {
            CrossBindingMethodInfo<global::LayoutScript> msetScript_0 = new CrossBindingMethodInfo<global::LayoutScript>("setScript");
            CrossBindingMethodInfo<global::myUIObject, System.String> massignWindow_1 = new CrossBindingMethodInfo<global::myUIObject, System.String>("assignWindow");
            CrossBindingMethodInfo<global::myUIObject, global::myUIObject, System.String> massignWindow_2 = new CrossBindingMethodInfo<global::myUIObject, global::myUIObject, System.String>("assignWindow");
            CrossBindingMethodInfo minit_3 = new CrossBindingMethodInfo("init");
            CrossBindingMethodInfo mdestroy_4 = new CrossBindingMethodInfo("destroy");
            CrossBindingMethodInfo mreset_5 = new CrossBindingMethodInfo("reset");
            CrossBindingMethodInfo mrecycle_6 = new CrossBindingMethodInfo("recycle");
            CrossBindingMethodInfo mresetProperty_7 = new CrossBindingMethodInfo("resetProperty");

            bool isInvokingToString;
            ILTypeInstance instance;
            ILRuntime.Runtime.Enviorment.AppDomain appdomain;

            public Adapter()
            {

            }

            public Adapter(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
            {
                this.appdomain = appdomain;
                this.instance = instance;
            }

            public ILTypeInstance ILInstance { get { return instance; } }

            public override void setScript(global::LayoutScript script)
            {
                if (msetScript_0.CheckShouldInvokeBase(this.instance))
                    base.setScript(script);
                else
                    msetScript_0.Invoke(this.instance, script);
            }

            public override void assignWindow(global::myUIObject parent, System.String name)
            {
                if (massignWindow_1.CheckShouldInvokeBase(this.instance))
                    base.assignWindow(parent, name);
                else
                    massignWindow_1.Invoke(this.instance, parent, name);
            }

            public override void assignWindow(global::myUIObject parent, global::myUIObject template, System.String name)
            {
                if (massignWindow_2.CheckShouldInvokeBase(this.instance))
                    base.assignWindow(parent, template, name);
                else
                    massignWindow_2.Invoke(this.instance, parent, template, name);
            }

            public override void init()
            {
                if (minit_3.CheckShouldInvokeBase(this.instance))
                    base.init();
                else
                    minit_3.Invoke(this.instance);
            }

            public override void destroy()
            {
                if (mdestroy_4.CheckShouldInvokeBase(this.instance))
                    base.destroy();
                else
                    mdestroy_4.Invoke(this.instance);
            }

            public override void reset()
            {
                if (mreset_5.CheckShouldInvokeBase(this.instance))
                    base.reset();
                else
                    mreset_5.Invoke(this.instance);
            }

            public override void recycle()
            {
                if (mrecycle_6.CheckShouldInvokeBase(this.instance))
                    base.recycle();
                else
                    mrecycle_6.Invoke(this.instance);
            }

            public override void resetProperty()
            {
                if (mresetProperty_7.CheckShouldInvokeBase(this.instance))
                    base.resetProperty();
                else
                    mresetProperty_7.Invoke(this.instance);
            }

            public override string ToString()
            {
                IMethod m = appdomain.ObjectType.GetMethod("ToString", 0);
                m = instance.Type.GetVirtualMethod(m);
                if (m == null || m is ILMethod)
                {
                    if (!isInvokingToString)
                    {
                        isInvokingToString = true;
                        string res = instance.ToString();
                        isInvokingToString = false;
                        return res;
                    }
                    else
                        return instance.Type.FullName;
                }
                else
                    return instance.Type.FullName;
            }
        }
    }
}