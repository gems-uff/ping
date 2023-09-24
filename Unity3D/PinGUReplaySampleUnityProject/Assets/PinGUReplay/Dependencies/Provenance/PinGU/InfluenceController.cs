using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PinGU { 
    public class InfluenceController : MonoBehaviour
    {
        //=================================================================================================================
        // Script for storing influence edges for the entire game
        // Attach this script in an Empty GameObject that is never destroyed during the game 
        //(In the same GameObject for ProvenanceGatherer)
        // Link it to ProvenanceGatherer
        //
        // Uses ArrayList for influence edges
        // All functions are automatically invoked and controlled by 'ExtractProvenance' script script
        //
        // If you desire to manually clean/erase the influence list, then invoke 'CleanInfluence' function
        //=================================================================================================================

        //=================================================================================================================
        // *Declarations Section*
        //=================================================================================================================
        public ProvenanceController provenance;
        private List<InfluenceEdge> influenceList = new List<InfluenceEdge>();
        private List<InfluenceEdge> consumableList = new List<InfluenceEdge>();

        //=================================================================================================================
        // *Functions Section*
        //=================================================================================================================

        //=================================================================================================================
        // Create a new influence and add it to the influence list
        // Function invoked at 'ExtractProvenance' to create a new influence
        //=================================================================================================================

        public void CreateInfluence(string tag, string ID, string source, string influenceName, string influenceValue, bool consumable, int quantity, GameObject target, float expirationTime)
        {
            InfluenceEdge newInfluence;
            if (target != null)
            {
                // Create Missable Influence and associate the edge with the GameObject
                ExtractProvenance prov = target.GetComponent<ExtractProvenance>();
                string missableID = provenance.CreateInfluenceEdge(source, prov.GetCurrentVertex().ID, influenceName + " Missed", "0");

                // Create the normal Influence that will replace the missable edge when consumed
                newInfluence = new InfluenceEdge(tag, ID, source, influenceName, influenceValue, consumable, quantity, missableID, expirationTime);
            }
            else
                newInfluence = new InfluenceEdge(tag, ID, source, influenceName, influenceValue, consumable, quantity, expirationTime);

            if (consumable)
                consumableList.Add(newInfluence);
            else
                influenceList.Add(newInfluence);
        }

        //=================================================================================================================
        // Remove all influences from the influence list
        // Use this function to remove all influences in the influence list
        //=================================================================================================================
        public void CleanInfluence()
        {
            CleanInfluenceConsumable();
            CleanInfluenceNotConsumable();
        }

        public void CleanInfluenceConsumable()
        {
            influenceList = new List<InfluenceEdge> ();
        }
        public void CleanInfluenceNotConsumable()
        {
            influenceList = new List<InfluenceEdge> ();
        }

        //=================================================================================================================
        // Remove all influences with 'tag' from the influence list
        // Function invoked at 'ExtractProvenance' to remove an existing influence because it expired
        //=================================================================================================================
        public void RemoveInfluenceByTag(string tag)
        {
            RemoveInfluenceByTag(tag, influenceList);
            RemoveInfluenceByTag(tag, consumableList);
        }

        public void RemoveInfluenceByTag(string tag, List<InfluenceEdge> list)
        {
            int i;
            //InfluenceEdge currentInf = new InfluenceEdge();
            for (i = 0; i < list.Count; i++)
            {
                //currentInf = list[i];
                if (list[i].tag == tag)
                {
                    list.RemoveAt(i);
                }
            }
        }

        //=================================================================================================================
        // Remove all influences with 'ID' from the influence list
        // Function invoked at 'ExtractProvenance' to remove an existing influence because it expired
        //=================================================================================================================
        public void RemoveInfluenceByID(string ID)
        {
            RemoveInfluenceByID(ID, influenceList);
            RemoveInfluenceByID(ID, consumableList);
        }

        public void RemoveInfluenceByID(string ID, List<InfluenceEdge> list)
        {
            int i;
            for (i = 0; i < list.Count; i++)
            {
                if (list[i].ID == ID)
                {
                    list.RemoveAt(i);
                }
            }
        }

        //=================================================================================================================
        // Check if there are any influences for the current vertex
        // Function invoked at 'ExtractProvenance' to check if the current action was influenced
        //=================================================================================================================

        // Check influence list by 'tag'
        public void WasInfluencedByTag(string tag, string targetID)
        {
            WasInfluencedBy(tag, targetID, true);
        }

        // Check influence list by influence's 'ID'
        public void WasInfluencedByID(string ID, string targetID)
        {
            WasInfluencedBy(ID, targetID, false);
        }


        // Check both influence lists
        public void WasInfluencedBy(string type, string targetID, bool isTag)
        {
            // Normal influence List
            WasInfluencedBy(type, targetID, influenceList, isTag);
            // Consumable influence List
            WasInfluencedBy(type, targetID, consumableList, isTag);
        }

        public void WasInfluencedBy(string type, string targetID, List<InfluenceEdge> list, bool isTag)
        {
            int i;
            string edgeValue;

            Stack<int> stack = new Stack<int>();

            for (i = 0; i < list.Count; i++)
            {
                // Determine if the search is by TAG or ID
                if (isTag)
                    edgeValue = list[i].tag;
                else
                    edgeValue = list[i].ID;

                if (edgeValue == type)
                {
                    list[i].quantity--;
                    // Check if this influence had expiration time
                    if ((list[i].expirationTime == -1) || (Time.time < (list[i].expirationTime)))
                    {
                        // Check if the influence had a missable placeholder
                        if (list[i].missableID != null)
                        {
                            // This influence had a missable placeholder
                            // Need to update the placeholder instead of adding a new edge
                            provenance.UpdateInfluenceEdge(list[i].source, targetID, list[i].type, list[i].infValue, list[i].missableID);
                        }
                        else
                        {
                            provenance.CreateInfluenceEdge(list[i].source, targetID, list[i].type, list[i].infValue);
                        }

                        if ((list[i].quantity <= 0) && (list[i].consumable))
                        {
                            //list.RemoveAt(i);
                            stack.Push(i);
                        }
                    }
                    else    //Remove it since it expired
                            //list.RemoveAt(i);
                        stack.Push(i);
                }
            }

            int stackSize = stack.Count;
            for (i = 0; i < stackSize; i++)
            {
                list.RemoveAt(stack.Pop());
            }
        }

        /*
        void  Awake (){
            provenance = GetComponentInChildren<ProvenanceController>();
        }
        */
    }
}