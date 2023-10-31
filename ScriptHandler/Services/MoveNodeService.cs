#define _USE_OLD_DIAGRAM
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using ScriptHandler.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ScriptHandler.Services
{
	public class MoveNodeService
	{
		

		public bool MoveNode(
			ScriptNodeBase dropped_scriptNodeBase,
			int indexOfDroppedOn,
			ObservableCollection<IScriptItem> scriptNodeList,
#if _USE_OLD_DIAGRAM
			ScriptDiagramViewModel scriptDiagram
#else // _USE_OLD_DIAGRAM
			SingleScriptDiagramService scriptDiagram
#endif // _USE_OLD_DIAGRAM
			)
		{
			int indexOfDroppedItem = scriptNodeList.IndexOf(dropped_scriptNodeBase);

			if (indexOfDroppedOn == indexOfDroppedItem)
				return false;

			List<IScriptItem> list = scriptNodeList.ToList().Where((i) => ((ScriptNodeBase)i).FailNextId == dropped_scriptNodeBase.ID).ToList();
			

			IScriptItem droppedItem = dropped_scriptNodeBase;
			IScriptItem droppedOn = null;
			if(indexOfDroppedOn > -1)
				droppedOn = scriptNodeList[indexOfDroppedOn];
			scriptNodeList.RemoveAt(indexOfDroppedItem);

			if (indexOfDroppedOn > -1)
				scriptNodeList.Insert(indexOfDroppedOn, droppedItem);
			else
				scriptNodeList.Add(droppedItem);


			SetPassNextInNewLocation(
				indexOfDroppedOn,
				scriptNodeList);

			SetPassNextInOldLocation(
				indexOfDroppedOn,
				indexOfDroppedItem,
				scriptNodeList);

			foreach (IScriptItem item in list)
			{
				if (item is ScriptNodeBase nodeBase)
					nodeBase.FailNext = droppedItem;
			}




			if (scriptDiagram != null)
			{
				scriptDiagram.MoveNode(
					droppedItem,
					droppedOn);
			}



			return true;
		}

		private void SetPassNextInNewLocation(
			int indexOfDroppedOn,
			ObservableCollection<IScriptItem> scriptNodeList)
		{
			if(indexOfDroppedOn > 0)
				scriptNodeList[indexOfDroppedOn - 1].PassNext = scriptNodeList[indexOfDroppedOn];

			if (indexOfDroppedOn >= 0 && indexOfDroppedOn < (scriptNodeList.Count - 1))
			{
				scriptNodeList[indexOfDroppedOn].PassNext = scriptNodeList[indexOfDroppedOn + 1];
			}
			else if (indexOfDroppedOn == (scriptNodeList.Count - 1))
			{
				scriptNodeList[scriptNodeList.Count - 1].PassNext = null;
			}
				
		}

		private void SetPassNextInOldLocation(
			int indexOfDroppedOn,
			int indexOfDroppedItem,
			ObservableCollection<IScriptItem> scriptNodeList)
		{
			if (indexOfDroppedItem == 0)
				return;

			if (indexOfDroppedItem == (scriptNodeList.Count - 1))
				scriptNodeList[indexOfDroppedItem].PassNext = null;
			else if (indexOfDroppedOn < indexOfDroppedItem)
				scriptNodeList[indexOfDroppedItem].PassNext = scriptNodeList[indexOfDroppedItem + 1];
			else
				scriptNodeList[indexOfDroppedItem - 1].PassNext = scriptNodeList[indexOfDroppedItem];
		}


	}
}
