using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace IVCE.DAI.Common.Helpers
{
    public static class EmailHelper
    {

        public static string GenerateNextEmailUserSequenceNumber(string email)
        {
            // string s = "Test.User1@41wn2f.onmicrosoft.com";
           // string email = "Test.User12@41wn2f.onmicrosoft.com";

            string username = email.Substring(0, email.IndexOf('@'));

            var currentSeqNumber = Regex.Replace(username, "[^0-9]", "");
            int GetNewEmailSeqNumber(int numberOnly) => numberOnly > 0 ? numberOnly += 1 : 0;

            var newSeqNum = !string.IsNullOrEmpty(currentSeqNumber) ? GetNewEmailSeqNumber(int.Parse(currentSeqNumber)) : 0;

            if (newSeqNum > 0)
            {
                var newUserNameWOSeqNum = Regex.Replace(username, "[0-9]", "");
                var newUsername = string.Concat(newUserNameWOSeqNum, newSeqNum);
                var hostname = email.Substring(email.IndexOf('@'), email.Length - email.IndexOf('@'));

                return string.Concat(newUsername, hostname);

            }

            return email;

        }

        public static string FormatNameParts(string fullnameParts)
        {

            string[] nameparts = fullnameParts.Split(' ');//Represents an array of strings separated by spaces

            var namepart = nameparts.Length == 1 ? nameparts[0] : nameparts[nameparts.Length - nameparts.Length]; // get first namepart element

            char[] toReplace = "àèìòùÀÈÌÒÙ äëïöüÄËÏÖÜ âêîôûÂÊÎÔÛ áéíóúÁÉÍÓÚðÐýÝ ãñõÃÑÕšŠžŽçÇåÅøØ".ToCharArray();
            char[] replaceChars = "aeiouAEIOU aeiouAEIOU aeiouAEIOU aeiouAEIOUdDyY anoANOsSzZcCaAoO".ToCharArray();


            for (int i = 0; i < toReplace.GetUpperBound(0); i++)
            {
                namepart = namepart.Replace(toReplace[i], replaceChars[i]);
            }
            return namepart;

        }
    }
}
