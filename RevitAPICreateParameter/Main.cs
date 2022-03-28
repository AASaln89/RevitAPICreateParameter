using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPICreateParameter
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            var categorySet = new CategorySet();
            categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeCurves));

            using (Transaction ts = new Transaction(doc, "add parameters"))
            {
                ts.Start();
                CreateSharedParameter(uiapp.Application, doc, "Длина с запасом", categorySet, BuiltInParameterGroup.PG_LENGTH, true);
                ts.Commit();
            }
            IList<Reference> selectedElementsRefList = uidoc.Selection.PickObjects(ObjectType.Face, "Select elem");
            var elementList = new List<Element>();
            double sum = 0;
            double stLength = 1.1;
            foreach (var selectedElement in selectedElementsRefList)
            {
                Element element = doc.GetElement(selectedElement);
                if (element is Pipe)
                {
                    
                    using (Transaction ts1 = new Transaction(doc, "set parameter"))
                    {
                        ts1.Start();
                        var familyinstance = element as FamilyInstance;
                        Parameter lengthParameter = familyinstance.GetParameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                        lengthParameter.AsDouble().ToString();
                        sum += lengthParameter * stLength;
                        lengthParameter.Set(sum);
                        ts1.Commit();
                    }
                }
            }
            TaskDialog.Show("Сообщение", sum.ToString());
            return Result.Succeeded;
        }

        private void CreateSharedParameter(Application application, Document doc, string parameterName, 
            CategorySet categorySet, BuiltInParameterGroup builtInParameterGroup, bool isInstance)
        {
            DefinitionFile definitionFile = application.OpenSharedParameterFile();

            if (definitionFile == null)
            {
                TaskDialog.Show("Ошибка", "Не найден ФОП");
                return;
            }

            Definition definition = definitionFile.Groups
                .SelectMany(group => group.Definitions)
                .FirstOrDefault(def => def.Name.Equals(parameterName));
            if (definition == null)
            {
                TaskDialog.Show("Ошибка", "Не найден параметр");
                return;
            }

            Binding binding = application.Create.NewTypeBinding(categorySet);
            if (isInstance)
            {
                binding = application.Create.NewInstanceBinding(categorySet);
            }

            BindingMap map = doc.ParameterBindings;
            map.Insert(definition, binding, builtInParameterGroup);
        }
    }
}
