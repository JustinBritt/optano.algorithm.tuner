﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// Dieser Quellcode wurde automatisch generiert von xsd, Version=4.6.1055.0.
// 
namespace Optano.Algorithm.Tuner.ParameterTreeReader.Elements {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(ParameterNode))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(value))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(or))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(and))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("node", Namespace="", IsNullable=false)]
    public abstract partial class Node {
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class Choice {
        
        private object itemField;
        
        private Node childField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("boolean", typeof(bool))]
        [System.Xml.Serialization.XmlElementAttribute("double", typeof(double))]
        [System.Xml.Serialization.XmlElementAttribute("int", typeof(int))]
        [System.Xml.Serialization.XmlElementAttribute("string", typeof(string))]
        public object Item {
            get {
                return this.itemField;
            }
            set {
                this.itemField = value;
            }
        }
        
        /// <remarks/>
        public Node child {
            get {
                return this.childField;
            }
            set {
                this.childField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(categorical))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(discrete))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(continuous))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public abstract partial class Domain {
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class categorical : Domain {
        
        private double[] doublesField;
        
        private string[] stringsField;
        
        private int[] intsField;
        
        private bool[] booleansField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public double[] doubles {
            get {
                return this.doublesField;
            }
            set {
                this.doublesField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string[] strings {
            get {
                return this.stringsField;
            }
            set {
                this.stringsField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int[] ints {
            get {
                return this.intsField;
            }
            set {
                this.intsField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool[] booleans {
            get {
                return this.booleansField;
            }
            set {
                this.booleansField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class discrete : Domain {

        private bool logField;

        private int startField;
        
        private int endField;

        public discrete()
        {
            this.logField = false;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool log
        {
            get
            {
                return this.logField;
            }
            set
            {
                this.logField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int start {
            get {
                return this.startField;
            }
            set {
                this.startField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int end {
            get {
                return this.endField;
            }
            set {
                this.endField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class continuous : Domain {
        
        private bool logField;
        
        private double startField;
        
        private double endField;
        
        public continuous() {
            this.logField = false;
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool log {
            get {
                return this.logField;
            }
            set {
                this.logField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public double start {
            get {
                return this.startField;
            }
            set {
                this.startField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public double end {
            get {
                return this.endField;
            }
            set {
                this.endField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(value))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(or))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public abstract partial class ParameterNode : Node {
        
        private Domain domainField;
        
        private string idField;
        
        /// <remarks/>
        public Domain domain {
            get {
                return this.domainField;
            }
            set {
                this.domainField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id {
            get {
                return this.idField;
            }
            set {
                this.idField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class value : ParameterNode {
        
        private Node nodeField;
        
        /// <remarks/>
        public Node node {
            get {
                return this.nodeField;
            }
            set {
                this.nodeField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class or : ParameterNode {
        
        private Choice[] choiceField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("choice")]
        public Choice[] choice {
            get {
                return this.choiceField;
            }
            set {
                this.choiceField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class and : Node {
        
        private Node[] nodeField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("node")]
        public Node[] node {
            get {
                return this.nodeField;
            }
            set {
                this.nodeField = value;
            }
        }
    }
}
