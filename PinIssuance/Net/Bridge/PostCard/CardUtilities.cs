using PrimeUtility.BussinessObjects;
using PrimeUtility.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PinIssuance.Net.Bridge.PostCard
{
    public class CardUtilities
    {
        public static string MaskPan(string cardPan)
        {
            string mask = "";
            if (!string.IsNullOrEmpty(cardPan))
            {
                char[] charArray = cardPan.ToCharArray();
                for (int i = 0; i < charArray.Length; i++)
                {
                    mask += i > 5 && i < charArray.Length - 6 ? "*" : charArray[i].ToString();
                }
            }
            return mask;
        }
        public static string GetCardPinOffset(string cardPan, string cardTable)
        {
            string pinoffset = string.Empty;

            string query = string.Format("SELECT TOP 1 pvv_or_pin_offset FROM {0} where pan = '{1}' order by expiry_date desc", cardTable, cardPan);
            System.Data.SqlClient.SqlConnection cn = new System.Data.SqlClient.SqlConnection(PrimeUtility.Configuration.ConfigurationManager.ProcessorConnections["PostCard"]);
            System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(query, cn);

            cn.Open();

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    pinoffset = reader[0].ToString();
                }
            }
            return pinoffset;

        }
        public static bool UpdateCardPinOffset(Card theCard, string pinOffset)
        {
            Card Card = GetCardFromPostCard(theCard.pan, "pc_cards_1_A");
            string table = "pc_cards_1_A";


            // string tableA = string.Format("pc_cards_{0}_A", Card.issuer_nr); //formulate the tablename based on issuer nr
            //string tableB = string.Format("pc_cards_{0}_B", Card.issuer_nr); //formulate the tablename based on issuer nr
            //string table = "";

            //confirm the card exist on this table
            /*  if (GetCardFromPostCard(theCard.pan, tableA) != null)
              {
                  table = tableA;
              }
              else
              {
                  table = tableB;
              }*/

            string updateQ = string.Format("Update {0} set pvv_or_pin_offset='{1}', card_status= 0 where pan='{2}' and expiry_date='{3}' and seq_nr='{4}'", table, pinOffset, theCard.pan, theCard.expiry_date, theCard.seq_nr);
            new PANE.ERRORLOG.Error().LogInfo("pin offset update query =" + updateQ);

            new PrimeUtility.BaseDAO.CustomCoreDAO(ProcessorType.PostCard_MSSQL).RunSQL(updateQ);

            return true;
        }
        public static bool UpdateCardPinOffset_ActiveActive(Card theCard, string pinOffset)
        {
            Card Card = GetCardFromPostCard(theCard.pan, "pc_cards_1_A");
            string table = "pc_cards_1_A";


            // string tableA = string.Format("pc_cards_{0}_A", Card.issuer_nr); //formulate the tablename based on issuer nr
            //string tableB = string.Format("pc_cards_{0}_B", Card.issuer_nr); //formulate the tablename based on issuer nr
            //string table = "";

            //confirm the card exist on this table
            /*  if (GetCardFromPostCard(theCard.pan, tableA) != null)
              {
                  table = tableA;
              }
              else
              {
                  table = tableB;
              }*/

            string updateQ = string.Format("Update {0} set pvv_or_pin_offset='{1}', card_status= 0 where pan='{2}' and expiry_date='{3}' and seq_nr='{4}'", table, pinOffset, theCard.pan, theCard.expiry_date, theCard.seq_nr);
            new PANE.ERRORLOG.Error().LogInfo("pin offset update query =" + updateQ);

            new PrimeUtility.BaseDAO.CustomCoreDAO(ProcessorType.PostCard_MSSQL).RunSQL(updateQ);
            new PrimeUtility.BaseDAO.CustomCoreDAO(ProcessorType.PostCard_MSSQL_2).RunSQL(updateQ);

            return true;
        }
        public static Card GetCardFromPostCard(string pan, string CardTableName)
        {
            Card cardCrit = new Card() { pan = pan };

            IList<Card> _cards = new PrimeUtility.BaseDAO.CustomCoreDAO(ProcessorType.PostCard_MSSQL).Retrieve<Card>(cardCrit, CardTableName);
            if (_cards == null)
                throw new ApplicationException(string.Format("Invalid card PAN {0}", MaskPan(pan)));
            Card theCard = _cards.OrderByDescending(x => Convert.ToInt32(x.expiry_date)).First();
            if (theCard == null)
                throw new ApplicationException("Inactive Card");
            return theCard;
        }
        public static Card RetrieveCard(string pan, string expiryDate)
        {
            // Do a card check on PostCard to ensure that the card exists
            Card cardCrit = new Card() { pan = pan, expiry_date = expiryDate };
            string query = string.Format(PrimeUtility.Configuration.ConfigurationManager.GetCardQuery, pan, expiryDate);
            new PANE.ERRORLOG.Error().LogInfo("Conn String: " + PrimeUtility.Configuration.ConfigurationManager.GetConString);
            new PANE.ERRORLOG.Error().LogInfo("Card Query: " + query);
            IList<Card> _cards = new PrimeUtility.BaseDAO.CustomCoreDAO(ProcessorType.PostCard_MSSQL).RetrieveList<Card>(query);

            //IList<Card> _cards = new PostCardEntitySystem().RetrieveCard(pan, null); // HINT.UFO: PostCardEntities
            if (_cards == null) throw new ApplicationException(string.Format("Invalid Card PAN {0}", MaskPan(pan)));
            Card theCard = _cards.OrderByDescending(x => Convert.ToInt32(x.expiry_date)).First();
            //if (theCard == null) throw new POSMessageProcessingException("In-active card");
            return theCard;
        }
        public static Card RetrieveCard(string pan, string expiryDate, string CardTableName)
        {
            Card cardCrit = new Card() { pan = pan, expiry_date = expiryDate };
            IList<Card> result = new PrimeUtility.BaseDAO.CustomCoreDAO(ProcessorType.PostCard_MSSQL).Retrieve<Card>(cardCrit, CardTableName);
            if (result != null && result.Count > 0)
            {
                Card theCard = result.OrderByDescending(x => Convert.ToInt32(x.expiry_date)).First();
                return theCard;
            }
            return null;
        }
    }
}
