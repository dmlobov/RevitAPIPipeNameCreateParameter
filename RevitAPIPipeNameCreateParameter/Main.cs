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
using Autodesk.Revit.ApplicationServices;

namespace RevitAPIPipeNameCreateParameter
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

            using (Transaction ts = new Transaction(doc, "Add parameter"))
            {
                ts.Start();
                CreateSharedParameter(uiapp.Application, doc, "Наименование", categorySet, BuiltInParameterGroup.PG_IDENTITY_DATA, true);
                ts.Commit();
            }

            var selectedRef = uidoc.Selection.PickObject(ObjectType.Element, "Выберете элементы");
            var selectedElement = doc.GetElement(selectedRef);
            if (selectedElement is Pipe)
            {
                using (Transaction ts = new Transaction(doc, "Set parameters"))
                {
                    ts.Start();
                    var pipeParameter = selectedElement as Pipe;
                    Parameter ndiam = pipeParameter.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER);
                    Parameter vdiam = pipeParameter.get_Parameter(BuiltInParameter.RBS_PIPE_INNER_DIAM_PARAM);
                    Parameter nameDiam = pipeParameter.LookupParameter("Наименование");
                    string narDiam = ndiam.ToString();
                    string vnDiam = vdiam.ToString();
                    nameDiam.Set($"{narDiam} / {vnDiam}");
                    ts.Commit();
                }
            }
            return Result.Succeeded;
                       
        }

        private void CreateSharedParameter(Application application,
            Document doc, string parameterName, CategorySet categorySet,
            BuiltInParameterGroup builtInParameterGroup, bool isInstance)
        {
            DefinitionFile definitionFile = application.OpenSharedParameterFile();
            if (definitionFile == null)
            {
                TaskDialog.Show("Ошибка", "Не найден файл общих параметров");
                return;
            }

            Definition definition = definitionFile.Groups
                .SelectMany(group => group.Definitions)
                .FirstOrDefault(def => def.Name.Equals(parameterName));
            if (definition == null)
            {
                TaskDialog.Show("Ошибка", "Не найден указанный параметр");
                return;
            }

            Binding binding = application.Create.NewTypeBinding(categorySet);
            if (isInstance)
                binding = application.Create.NewInstanceBinding(categorySet);

            BindingMap map = doc.ParameterBindings;
            map.Insert(definition, binding, builtInParameterGroup);
        }
    }
}
