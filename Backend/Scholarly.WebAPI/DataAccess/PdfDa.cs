using Scholarly.DataAccess;
using Scholarly.Entity;
using System;

namespace Scholarly.WebAPI.DataAccess
{
    public interface IPdfDa
    {
        bool DeleteGroupEmail(SWBDBContext swbDBContext, string UserId, int GroupEmailId);
    }
    public class PdfDa : IPdfDa
    {
        public bool DeleteGroupEmail(SWBDBContext swbDBContext, string UserId, int groupEmailId)
        {
            tbl_groups_emails? retult = swbDBContext.tbl_groups_emails.FirstOrDefault(p => p.group_email_id == groupEmailId);
                if (retult != null)
            {
                retult.status = new bool?(true);
                retult.updated_by = UserId;
                retult.updated_date = new DateTime?(DateTime.Now);
                swbDBContext.SaveChanges();
                return true;
            }
            return false;
        }
    }
}
