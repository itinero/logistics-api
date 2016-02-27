// Itinero - OpenStreetMap (OSM) SDK
// Copyright (C) 2016 Abelshausen Ben
// 
// This file is part of Itinero.
// 
// Itinero is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// Itinero is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Itinero. If not, see <http://www.gnu.org/licenses/>.

using Nancy;
using Nancy.Json;
using Nancy.ModelBinding;
using Itinero;
using Itinero.Profiles;
using System;
using System.Collections.Generic;
using Itinero.LocalGeo;
using Itinero.Attributes;

namespace Itinero.Logistics.API.TSP
{
    /// <summary>
    /// The TSP module.
    /// </summary>
    public class TSPModule : NancyModule
    {
        /// <summary>
        /// Creates a new routing module.
        /// </summary>
        public TSPModule()
        {
            JsonSettings.MaxJsonLength = Int32.MaxValue;

            Get["{instance}/tsp"] = _ =>
            {
                return this.DoRouting(_);
            };
            Put["{instance}/tsp"] = _ =>
            {
                return this.DoRouting(_);
            };
        }

        /// <summary>
        /// Executes a routing request.
        /// </summary>
        /// <returns></returns>
        private dynamic DoRouting(dynamic _)
        {
            try
            {
                this.EnableCors();

                // validate requests.
                if (!Bootstrapper.ValidateRequest(this, _))
                {
                    return Negotiate.WithStatusCode(HttpStatusCode.Forbidden);
                }

                // get instance and check if active.
                string instance = _.instance;
                if (!TSPBootstrapper.IsActive(instance))
                { // oeps, instance not active!
                    return Negotiate.WithStatusCode(HttpStatusCode.NotFound);
                }

                // try and get all this data from the request-data.
                Coordinate[] coordinates = null;
                var profile = Itinero.Osm.Vehicles.Vehicle.Car.Fastest();
                bool? closed = null;
                var fullFormat = false;
                var attributes = new AttributeCollection[0];

                // bind the query if any.
                if (this.Request.Body == null || this.Request.Body.Length == 0)
                { // there is no body.
                    var urlParameterRequest = this.Bind<Domain.UrlParametersRequest>();
                    if (string.IsNullOrWhiteSpace(urlParameterRequest.loc))
                    { // no loc parameters.
                        return Negotiate.WithStatusCode(HttpStatusCode.NotAcceptable).WithModel("loc parameter not found or request invalid.");
                    }
                    var locs = urlParameterRequest.loc.Split(',');
                    if (locs.Length < 4)
                    { // less than two loc parameters.
                        return Negotiate.WithStatusCode(HttpStatusCode.NotAcceptable).WithModel("only one loc parameter found or request invalid.");
                    }
                    coordinates = new Coordinate[locs.Length / 2];
                    for (int idx = 0; idx < coordinates.Length; idx++)
                    {
                        float lat, lon;
                        if (float.TryParse(locs[idx * 2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lat) &&
                            float.TryParse(locs[idx * 2 + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lon))
                        { // parsing was successful.
                            coordinates[idx] = new Coordinate(lat, lon);
                        }
                        else
                        { // invalid formatting.
                            return Negotiate.WithStatusCode(HttpStatusCode.NotAcceptable).WithModel("location coordinates are invalid.");
                        }
                    }

                    // get vehicle.
                    string profileName = "car.fastest"; // assume car is the default.
                    if (!string.IsNullOrWhiteSpace(urlParameterRequest.profile))
                    { // a vehicle was defined.
                        profileName = urlParameterRequest.profile;
                    }
                    if (!Profile.TryGet(profileName, out profile))
                    {// vehicle not found or not registered.
                        return Negotiate.WithStatusCode(HttpStatusCode.NotAcceptable).WithModel(
                            string.Format("Profile with name '{0}' not found.", profileName));
                    }
                    if (!string.IsNullOrWhiteSpace(urlParameterRequest.closed))
                    { // there is a sort flag.
                        closed = urlParameterRequest.closed.ToLowerInvariant() == "true";
                    }

                    if (!string.IsNullOrWhiteSpace(urlParameterRequest.format))
                    { // there is a format field.
                        fullFormat = urlParameterRequest.format == "osmsharp";
                    }
                }
                else
                { // this should be a request with a json-body.
                    var request = this.Bind<Domain.Request>();

                    if (request.locations == null || request.locations.Length < 2)
                    { // less than two loc parameters.
                        return Negotiate.WithStatusCode(HttpStatusCode.NotAcceptable).WithModel("only one location found or request invalid.");
                    }
                    coordinates = new Coordinate[request.locations.Length];
                    for (int idx = 0; idx < coordinates.Length; idx++)
                    {
                        coordinates[idx] = new Coordinate(request.locations[idx][1], request.locations[idx][0]);
                    }

                    if (request.tags != null)
                    { // parse tags.
                        attributes = new AttributeCollection[request.tags.Length];
                        for (var i = 0; i < request.tags.Length; i++)
                        {
                            var locationTag = request.tags[i];
                            if (locationTag != null)
                            {
                                var locationTagCollection = new AttributeCollection();
                                for (var t = 1; t < locationTag.Length; t += 2)
                                {
                                    locationTagCollection.AddOrReplace(locationTag[0],
                                        locationTag[1]);
                                }
                                attributes[i] = locationTagCollection;
                            }
                        }
                    }

                    // get vehicle.
                    string profileName = "car"; // assume car is the default.
                    if (request.profile != null && !string.IsNullOrWhiteSpace(request.profile.name))
                    { // a vehicle was defined.
                        profileName = request.profile.name;
                    }
                    if (!Profile.TryGet(profileName, out profile))
                    { // vehicle not found or not registered.
                        return Negotiate.WithStatusCode(HttpStatusCode.NotAcceptable).WithModel(
                            string.Format("Profile with name '{0}' not found.", profileName));
                    }

                    if (!string.IsNullOrWhiteSpace(request.format))
                    { // there is a format field.
                        fullFormat = request.format == "osmsharp";
                    }

                    closed = request.closed;
                }

                // check for support for the given vehicle.
                if (!TSPBootstrapper.Get(instance).Supports(profile))
                { // vehicle is not supported.
                    return Negotiate.WithStatusCode(HttpStatusCode.NotAcceptable).WithModel(
                        string.Format("Profile with name '{0}' is unsupported by this instance.", profile.Name));
                }

                // calculate route.
                var route = TSPBootstrapper.Get(instance).Calculate(profile, coordinates, attributes, closed,
                    new Dictionary<string, object>());
                if (route == null ||
                    route.IsError)
                { // route could not be calculated.
                    return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError);
                }

                if (fullFormat)
                { // return a complete route but no instructions.
                    return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(route.Value);
                }
                else
                { // return a GeoJSON object.                    
                    var modalAggregator = new Itinero.Algorithms.Routes.RouteSegmentAggregator(
                    route.Value, Itinero.Algorithms.Routes.RouteSegmentAggregator.ModalAggregator);
                    modalAggregator.Run();
                    if (!modalAggregator.HasSucceeded)
                    {
                        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError);
                    }
                    return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(modalAggregator.AggregatedRoute);
                }
            }
            catch (Exception)
            { // an unhandled exception!
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError);
            }
        }
    }
}