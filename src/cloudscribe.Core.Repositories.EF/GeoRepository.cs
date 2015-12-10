﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:					Joe Audette
// Created:					2015-11-16
// Last Modified:			2015-12-10
// 

using cloudscribe.Core.Models.Geography;
using Microsoft.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cloudscribe.Core.Repositories.EF
{
    public class GeoRepository : IGeoRepository
    {

        public GeoRepository(CoreDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        private CoreDbContext dbContext;

        public async Task<bool> Save(IGeoCountry geoCountry)
        {
            if (geoCountry == null) { return false; }

            GeoCountry country = GeoCountry.FromIGeoCountry(geoCountry); // convert from IGeoCountry
            if (country.Guid == Guid.Empty)
            { 
                country.Guid = Guid.NewGuid();
                dbContext.Countries.Add(country);
            }
            else
            {
                bool tracking = dbContext.ChangeTracker.Entries<GeoCountry>().Any(x => x.Entity.Guid == country.Guid);
                if (!tracking)
                {
                    dbContext.Countries.Update(country);
                }

            }
            
            int rowsAffected = await dbContext.SaveChangesAsync();

            return rowsAffected > 0;
        }

        public async Task<IGeoCountry> FetchCountry(Guid guid)
        {
            GeoCountry item 
                = await dbContext.Countries.SingleOrDefaultAsync(x => x.Guid == guid);

            return item;
        }

        public async Task<IGeoCountry> FetchCountry(string isoCode2)
        {
            return await dbContext.Countries.SingleOrDefaultAsync(x => x.ISOCode2 == isoCode2);
        }

        public async Task<bool> DeleteCountry(Guid guid)
        {
            var result = false;
            var itemToRemove = await dbContext.Countries.SingleOrDefaultAsync(x => x.Guid == guid);
            if (itemToRemove != null)
            {
                dbContext.Countries.Remove(itemToRemove);
                int rowsAffected = await dbContext.SaveChangesAsync();
                result = rowsAffected > 0;
            }

            return result;

        }

        public async Task<int> GetCountryCount()
        {
            return await dbContext.Countries.CountAsync<GeoCountry>();
        }

        public async Task<List<IGeoCountry>> GetAllCountries()
        {
            var query = from c in dbContext.Countries
                        orderby c.Name ascending
                        select c;

            var items = await query.AsNoTracking().ToListAsync<IGeoCountry>();
            
            return items;

        }

        public async Task<List<IGeoCountry>> GetCountriesPage(int pageNumber, int pageSize)
        {
            int offset = (pageSize * pageNumber) - pageSize;

            var query = dbContext.Countries.OrderBy(x => x.Name) 
                .Select(p => p)
                .Skip(offset)
                .Take(pageSize)
                ;

         
           return await query.AsNoTracking().ToListAsync<IGeoCountry>();
            
        }

        public async Task<bool> Save(IGeoZone geoZone)
        {
            if (geoZone == null) { return false; }

            GeoZone state = GeoZone.FromIGeoZone(geoZone); // convert from IGeoZone

            if (geoZone.Guid == Guid.Empty)
            {
                state.Guid = Guid.NewGuid();
                dbContext.States.Add(state);
            }
            else
            {
                bool tracking = dbContext.ChangeTracker.Entries<GeoZone>().Any(x => x.Entity.Guid == state.Guid);
                if(!tracking)
                {
                    dbContext.States.Update(state);
                }
            }
            
            int rowsAffected = await dbContext.SaveChangesAsync();

            return rowsAffected > 0;

        }

        public async Task<IGeoZone> FetchGeoZone(Guid guid)
        {
            GeoZone item
                = await dbContext.States.SingleOrDefaultAsync(x => x.Guid == guid);

            return item;
        }

        public async Task<bool> DeleteGeoZone(Guid guid)
        {
            var result = false;
            var itemToRemove = await dbContext.States.SingleOrDefaultAsync(x => x.Guid == guid);
            if (itemToRemove != null)
            {
                dbContext.States.Remove(itemToRemove);
                int rowsAffected = await dbContext.SaveChangesAsync();
                result = rowsAffected > 0;
            }

            return result;
        }

        public async Task<bool> DeleteGeoZonesByCountry(Guid countryGuid)
        {
            var query = from l in dbContext.States
                        where l.CountryGuid == countryGuid
                        select l;

            dbContext.States.RemoveRange(query);
            int rowsAffected = await dbContext.SaveChangesAsync();
            return rowsAffected > 0;
        }

        public async Task<int> GetGeoZoneCount(Guid countryGuid)
        {
            return await dbContext.States.CountAsync<GeoZone>(
                g => g.CountryGuid == countryGuid);
        }

        public async Task<List<IGeoZone>> GetGeoZonesByCountry(Guid countryGuid)
        {
            //var query = from l in dbContext.States
            //            where l.CountryGuid == countryGuid
            //            orderby l.Name descending
            //            select l;

            var query = dbContext.States
                        .Where(x => x.CountryGuid == countryGuid)
                        .OrderByDescending(x => x.Name)
                        .Select(x => x);
            
            var items = await query.AsNoTracking().ToListAsync<IGeoZone>();
            return items;
            

        }

        public async Task<List<IGeoCountry>> CountryAutoComplete(string query, int maxRows)
        {
            // approximation of a LIKE operator query
            //http://stackoverflow.com/questions/17097764/linq-to-entities-using-the-sql-like-operator

            //var listQuery = from l in dbContext.Countries
            //                .Take(maxRows)
            //                where l.Name.Contains(query) || l.ISOCode2.Contains(query)
            //                orderby l.Name ascending
            //                select l;

            var listQuery = dbContext.Countries  
                            .Where(x =>  x.Name.Contains(query) || x.ISOCode2.Contains(query))
                            .OrderBy(x =>  x.Name)
                            .Take(maxRows)
                            .Select(x => x);

            var items = await listQuery.AsNoTracking().ToListAsync<IGeoCountry>();
            return items;
            
        }

        public async Task<List<IGeoZone>> StateAutoComplete(Guid countryGuid, string query, int maxRows)
        {
            //var listQuery = from l in dbContext.States
            //                .Take(maxRows)
            //                where (
            //                l.CountryGuid == countryGuid &&
            //                (l.Name.Contains(query) || l.Code.Contains(query))
            //                )
            //                orderby l.Code ascending
            //                select l;

            var listQuery = dbContext.States
                            .Where (x => 
                            x.CountryGuid == countryGuid &&
                            (x.Name.Contains(query) || x.Code.Contains(query))
                            )
                            .OrderBy(x => x.Code)
                            .Take(maxRows)
                            .Select(x => x);

            return await listQuery.AsNoTracking().ToListAsync<IGeoZone>();
           
        }

        public async Task<List<IGeoZone>> GetGeoZonePage(Guid countryGuid, int pageNumber, int pageSize)
        {
            int offset = (pageSize * pageNumber) - pageSize;
            
            var query = dbContext.States
               .Where(x => x.CountryGuid == countryGuid)
               .OrderBy(x => x.Name)
               .Skip(offset)
               .Take(pageSize)
               .Select(p => p)
               ;
            
            return await query.AsNoTracking().ToListAsync<IGeoZone>();
           
        }

        public async Task<bool> Save(ILanguage language)
        {
            if (language == null) { return false; }

            Language lang = Language.FromILanguage(language);

            if (lang.Guid == Guid.Empty)
            { 
                lang.Guid = Guid.NewGuid();
                dbContext.Languages.Add(lang);
            }
            else
            {
                bool tracking = dbContext.ChangeTracker.Entries<Language>().Any(x => x.Entity.Guid == lang.Guid);
                if (!tracking)
                {
                    dbContext.Languages.Update(lang);
                }
            }
            
            int rowsAffected = await dbContext.SaveChangesAsync();

            return rowsAffected > 0;

        }

        public async Task<ILanguage> FetchLanguage(Guid guid)
        {
            Language item
                = await dbContext.Languages.SingleOrDefaultAsync(x => x.Guid == guid);

            return item;
        }

        public async Task<bool> DeleteLanguage(Guid guid)
        {
            var result = false;
            var itemToRemove = await dbContext.Languages.SingleOrDefaultAsync(x => x.Guid == guid);
            if (itemToRemove != null)
            {
                dbContext.Languages.Remove(itemToRemove);
                int rowsAffected = await dbContext.SaveChangesAsync();
                result = rowsAffected > 0;
            }

            return result;
        }

        public async Task<int> GetLanguageCount()
        {
            return await dbContext.Languages.CountAsync<Language>();

        }

        public async Task<List<ILanguage>> GetAllLanguages()
        {
            var query = dbContext.Languages
                        .OrderBy(x => x.Name)
                        .Select(x => x);

            var items = await query.AsNoTracking().ToListAsync<ILanguage>();
            return items;
        
        }

        public async Task<List<ILanguage>> GetLanguagePage(int pageNumber, int pageSize)
        {
            int offset = (pageSize * pageNumber) - pageSize;
            
            var query = dbContext.Languages
                        .OrderBy(x => x.Name)
                        .Skip(offset)
                        .Take(pageSize)
                        .Select(x => x);
            
            return await query.AsNoTracking().ToListAsync<ILanguage>();
           
        }


        public async Task<bool> Save(ICurrency currency)
        {
            if (currency == null) { return false; }

            Currency c = Currency.FromICurrency(currency);
            if (c.Guid == Guid.Empty)
            { 
                c.Guid = Guid.NewGuid();
                dbContext.Currencies.Add(c);
            }
            else
            {
                bool tracking = dbContext.ChangeTracker.Entries<Currency>().Any(x => x.Entity.Guid == c.Guid);
                if (!tracking)
                {
                    dbContext.Currencies.Update(c);
                }
            }
            
            int rowsAffected = await dbContext.SaveChangesAsync();

            return rowsAffected > 0;

        }


        public async Task<ICurrency> FetchCurrency(Guid guid)
        {
            Currency item
                = await dbContext.Currencies.SingleOrDefaultAsync(x => x.Guid == guid);

            return item;
        }

        public async Task<bool> DeleteCurrency(Guid guid)
        {
            var result = false;
            var itemToRemove = await dbContext.Currencies.SingleOrDefaultAsync(x => x.Guid == guid);
            if (itemToRemove != null)
            {
                dbContext.Currencies.Remove(itemToRemove);
                int rowsAffected = await dbContext.SaveChangesAsync();
                result = rowsAffected > 0;
            }

            return result;

        }

        public async Task<List<ICurrency>> GetAllCurrencies()
        {
            
            var query = dbContext.Currencies
                        .OrderBy(x => x.Title)
                        .Select(x => x);

            var items = await query.AsNoTracking().ToListAsync<ICurrency>();
            return items;
           
        }


    }
}
