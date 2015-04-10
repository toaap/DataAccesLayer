using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgramKonstruktion.DAL
{
    
    //lol
    class DataAccessLayer
    {
        private ProgramKonstruktionEntities con = new ProgramKonstruktionEntities();

        //Lägger till lägenhet
        public string AddApartment(string appartmentNr)
        {
            string message = "";
            Apartment a = new Apartment();

            try
            {
                List<Apartment> apartmentList = con.Apartment.Where(r => r.apartmentNr == appartmentNr).ToList();

                if (apartmentList.Count() == 0)
                {
                    a.apartmentNr = appartmentNr;
                    con.Apartment.Add(a);
                    con.SaveChanges();
                    message = "Meddelande: Lägenhet tillagd!";
                    return message;
                }

            }catch(InvalidOperationException)
            {
                con.Apartment.Remove(a);
                throw new DatabaseException("Databasfel, kontakta systemadminstratör!");
            }

            message = "Meddelande: Lägenhet ej tillagd då lägenhet redan existerar!";
            return message;
        }

        //Tar bort lägenhet
        public string RemoveApartment(string apartmentNr)
        {
            string message = "";

            try
            {
                List<Apartment> apartmentList = con.Apartment.Where(r => r.apartmentNr == apartmentNr).ToList();

                if (apartmentList.Count() != 0)
                {
                    Apartment a = con.Apartment.First(r => r.apartmentNr == apartmentNr);
                    con.Apartment.Remove(a);
                    con.SaveChanges();
                    return message = "Meddelande: Lägenhet borttagen!";
                }
            }
            catch (InvalidOperationException)
            {
                throw new DatabaseException("Databasfel, kontakta systemadminstratör!");
            }
            message = "Meddelande: Borttagning misslyckades, lägenheten finns inte!";
            return message;
        }

        //Retunerar alla lägenheter
        public List<Apartment> GetAllApartments()
        {
            try
            {
                List<Apartment> apartmentList = con.Apartment.ToList();
                
                if (apartmentList.Count == 0)
                {
                    return null;
                }

                return apartmentList;
            }
            catch (InvalidOperationException)
            {
                throw new DatabaseException("Databasfel, kontakta systemadminstratör");
            }
        }

        //Bokningsgenomförande (kontroll av regler), själva bokningen sker i metoden AddBookingMethod
        public string AddBooking(Apartment apartment, string bookingDate, string laundryTimeString)
        {
            string message = "";
            bool hasBooking = false;
            LaundryTime laundryTime = new LaundryTime();

            try
            {
                laundryTime = con.LaundryTime.First(r => r.bookedLaundryTime == laundryTimeString);

                //Hämta info från databasen
                DateTime newBooking = DateTime.ParseExact(bookingDate + " " + laundryTime.bookedLaundryTime + "", "dd-MM-yyyy HH:mm", null);
                List<BookingList> oldList = con.BookingList.Where(r => r.Apartment.apartmentNr == apartment.apartmentNr).ToList();

                //1 - finns det en existerande bokning
                if (oldList.Count() != 0)
                {
                    var oldDate = oldList[0];
                    DateTime oldBooking = DateTime.ParseExact(oldDate.bookedDate + " " + oldDate.bookedLaundryTime + "", "dd-MM-yyyy HH:mm", null);

                    //1.1 Om bokningen har passerat dagens datum = radera
                    if (oldBooking < DateTime.Now)
                    {
                        RemoveBooking(apartment.apartmentNr);
                    }
                    //1.2 Om bokningen fortfarande är aktiv, alltså inte passerat dagens datum
                    else
                    {
                        hasBooking = true;
                    }
                }

                //2 Meddalande vid fel
                
                if (hasBooking)
                {
                    message = "Meddelande: Bokningen kunde ej genomföras, lägenheten har redan en aktiv bokning!";
                }
                if (!CheckAvailability(bookingDate, laundryTime))
                {
                    message = "Meddelande: Bokningen kunde ej genomföras, passet är redan bokat";
                }
                if (newBooking < DateTime.Now)
                {
                    message = "Meddelande: Bokningen kunde ej genomföras, tiden för passet har passerat";
                }

                //2.1 Bokar ny bokning
                if (newBooking > DateTime.Now && !hasBooking && CheckAvailability(bookingDate, laundryTime))
                {
                    AddBookingMethod(apartment, bookingDate, laundryTime);
                    message = "Meddelande: Bokningen genomförd!";
                }
                
                return message;

            }
            catch (InvalidOperationException)
            {
                throw new DatabaseException("Databasfel, kontakta systemadmistratör");
            }
        }

        //Används endast i AddBooking
        public bool CheckAvailability(string bookingDate, LaundryTime laundryTime)
        {
            try
            {
                List<BookingList> list = con.BookingList.Where(r => r.bookedDate == bookingDate && r.bookedLaundryTime == laundryTime.bookedLaundryTime).ToList();

                if (list.Count == 0)
                {
                    return true;
                }
                return false;
            }
            catch (InvalidOperationException)
            {
                throw new DatabaseException("Databasfel, kontakta systemadminstratör");
            }
        }

        //Används endast i AddBooking, utför bokningen
        public void AddBookingMethod(Apartment apartment, string bookingDate, LaundryTime laundryTime)
        {
            BookingList newBooking = new BookingList();
            newBooking.Apartment = apartment;
            newBooking.bookedDate = bookingDate;
            newBooking.LaundryTime = laundryTime;

            try
            {
                con.BookingList.Add(newBooking);
                con.SaveChanges();
            }
            catch (InvalidOperationException)
            {
                con.BookingList.Remove(newBooking);
                throw new DatabaseException("Databasfel, kontakta systemadminstratör!");
            }
           
        }

        //Tar bort bokning
        public string RemoveBooking(string apartmentNr)
        {
            string message = "";
            try
            {
                List<BookingList> bookingList = con.BookingList.Where(r => r.apartmentNr == apartmentNr).ToList();

                if (bookingList.Count() != 0)
                {
                    BookingList a = con.BookingList.First(r => r.apartmentNr == apartmentNr);
                    con.BookingList.Remove(a);
                    con.SaveChanges();
                    message = "Meddelande: Bokningen borttagen!";

                    return message;
                }
                message = "Meddelande: Borttagningen misslyckades, ingen bokning existerar!";
                return message;
            }
            catch (InvalidOperationException)
            {
                throw new DatabaseException("Databasfel, kontakta systemadminstratör");
            }
        }

        //Retunerar alla bokningar (Används ej i dagens gränssnitt, kan användas för adminpanel senare)
        public List<BookingList> GetBookingList(string bookedDate)
        {
            try
            {
                List<BookingList> oldBookingList = con.BookingList.Where(a => a.bookedDate == bookedDate).OrderBy(r => r.bookedLaundryTime).ToList();
                List<BookingList> newBookingList = new List<BookingList>();

                foreach (BookingList b in oldBookingList)
                {
                    DateTime oldBooking = DateTime.ParseExact(b.bookedDate + " " + b.bookedLaundryTime + "", "dd-MM-yyyy HH:mm", null);
                    if (oldBooking > DateTime.Now)
                    {
                        newBookingList.Add(b);
                    }
                }

                return newBookingList;
            }
            catch (InvalidOperationException)
            {
                throw new DatabaseException("Databasfel, kontakta systemadminstratör");
            }
        }

        //Retunerar en användares aktiva bokning
        public List<BookingList> GetSingleBooking(string apartmentNr)
        {
            try
            {
                List<BookingList> bookingList = con.BookingList.Where(r => r.apartmentNr == apartmentNr).ToList();
                List<BookingList> newBookingList = new List<BookingList>();

                if (bookingList.Count == 0)
                {
                    return null;
                }
                else
                {
                    foreach (BookingList b in bookingList)
                    {
                        BookingList tempBook = new BookingList();
                        DateTime oldBooking = DateTime.ParseExact(b.bookedDate + " " + b.bookedLaundryTime + "", "dd-MM-yyyy HH:mm", null);
                        if (oldBooking > DateTime.Now)
                        {
                            tempBook.Apartment = b.Apartment;
                            tempBook.apartmentNr = b.apartmentNr;
                            tempBook.bookedDate = b.bookedDate;
                            tempBook.bookedLaundryTime = b.LaundryTime.description;
                            tempBook.LaundryTime = b.LaundryTime;
                            newBookingList.Add(tempBook);
                        }
                    }

                    return newBookingList;
                }
            }
            catch (InvalidOperationException)
            {
                throw new DatabaseException("Databasfel, kontakta systemadminstratör");
            }

        }

        //Används bara i getAllLaundryTimes
        public List<BookingList> GetBookingListForLaundryTime(string bookedDate)
        {
            try
            {
                List<BookingList> oldBookingList = con.BookingList.Where(a => a.bookedDate == bookedDate).OrderBy(r => r.bookedLaundryTime).ToList();
                return oldBookingList;
            }
            catch (InvalidOperationException)
            {
                throw new DatabaseException("Databasfel, kontakta systemadminstratör");
            }
        }

        //Retunerar alla tvättpass som användaren kan interagera med i den färgglada listan
        public List<BookedHelper> GetAllLaundryTimes(string bookedDate)
        {
            try
            {
                List<LaundryTime> laundryTimeList = con.LaundryTime.ToList();
                List<BookingList> bookedDatesList = GetBookingListForLaundryTime(bookedDate);
                List<BookedHelper> newList = new List<BookedHelper>();
                string x = "";

                foreach (LaundryTime l in laundryTimeList)
                {
                    BookedHelper bh = new BookedHelper();
                    x = "Ledig";
                    bh.laundryTime = l.bookedLaundryTime;
                    bh.description = l.description;
                    bh.booked = x;
                    newList.Add(bh);
                }

                foreach (LaundryTime l in laundryTimeList)
                {
                    BookedHelper bh = new BookedHelper();
                    foreach (BookingList b in bookedDatesList)
                    {

                        if (l.bookedLaundryTime.Equals(b.bookedLaundryTime))
                        {
                            x = "Upptagen";
                            bh.laundryTime = l.bookedLaundryTime;
                            bh.booked = x;
                            bh.description = l.description;
                            BookedHelper itemToRemove = newList.Where(r => r.laundryTime == b.bookedLaundryTime).First();
                            newList.Remove(itemToRemove);
                            newList.Add(bh);
                        }

                    }
                }

                //kopia av newList, endast för att kunna redigera newList..
                List<BookedHelper> foreachListOfNewList = new List<BookedHelper>(newList);
                foreach (BookedHelper b in foreachListOfNewList)
                {
                    DateTime bookedDateTime = DateTime.ParseExact(bookedDate + " " + b.laundryTime + "", "dd-MM-yyyy HH:mm", null);
                    BookedHelper bh = new BookedHelper();

                    if (bookedDateTime < DateTime.Now)
                    {
                        x = "Tid passerat";
                        bh.laundryTime = b.laundryTime;
                        bh.booked = x;
                        bh.description = b.description;
                        BookedHelper itemtoremove = newList.Where(r => r.laundryTime == b.laundryTime).First();
                        newList.Remove(itemtoremove);
                        newList.Add(bh);
                    }
                }
                newList = newList.OrderBy(r => r.laundryTime).ToList();
                return newList;
            }
            catch (InvalidOperationException)
            {
                throw new DatabaseException("Databasfel, kontakta systemadminstratör");
            }
        }

    }
}
