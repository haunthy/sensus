// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using SensusService.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Geolocation;

namespace SensusService.Probes.Location
{
    /// <summary>
    /// A GPS receiver. Implemented as a singleton.
    /// </summary>
    public class GpsReceiver
    {
        #region static members
        public static GpsReceiver _singleton = new GpsReceiver();

        public static GpsReceiver Get()
        {
            return _singleton;
        }
        #endregion

        private Geolocator _locator;
        private int _desiredAccuracyMeters;
        private bool _readingIsComing;
        private ManualResetEvent _readingWait;
        private Position _reading;
        private int _readingTimeoutMS;

        private readonly object _locker = new object();

        public Geolocator Locator
        {
            get { return _locator; }
            set { _locator = value; }
        }

        public int DesiredAccuracyMeters
        {
            get { return _desiredAccuracyMeters; }
            set
            {
                _desiredAccuracyMeters = value;

                if (_locator != null)
                    _locator.DesiredAccuracy = _desiredAccuracyMeters;
            }
        }

        private GpsReceiver()
        {
            _desiredAccuracyMeters = 10;
            _readingIsComing = false;
            _readingWait = new ManualResetEvent(false);
            _reading = null;
            _readingTimeoutMS = 120000;
        }

        public void Initialize(Geolocator locator)
        {
            _locator = locator;
            _locator.DesiredAccuracy = _desiredAccuracyMeters;
        }

        public Position GetReading()
        {
            return GetReading(0);
        }

        public Position GetReading(int maxReadingAgeForReuseMS)
        {  
            lock (_locker)
            {
                // reuse existing reading if it isn't too old
                if (_reading != null && maxReadingAgeForReuseMS > 0)
                {
                    double readingAgeMS = (DateTimeOffset.UtcNow - _reading.Timestamp).TotalMilliseconds;
                    if (readingAgeMS <= maxReadingAgeForReuseMS)
                    {
                        SensusServiceHelper.Get().Logger.Log("Reusing previous GPS reading, which is " + readingAgeMS + "ms old (maximum = " + maxReadingAgeForReuseMS + "ms).", LoggingLevel.Verbose, GetType());
                        return _reading;
                    }
                }

                if (_readingIsComing)
                    SensusServiceHelper.Get().Logger.Log("A GPS reading is coming. Will wait for it.", LoggingLevel.Debug, GetType());
                else
                {
                    _readingIsComing = true;  // tell any subsequent, concurrent callers that we're taking a reading
                    _readingWait.Reset();  // make them wait

                    new Thread(async () =>
                        {
                            try
                            {
                                SensusServiceHelper.Get().Logger.Log("Taking GPS reading.", LoggingLevel.Debug, GetType());

                                DateTimeOffset readingStart = DateTimeOffset.UtcNow;
                                Position newReading = await _locator.GetPositionAsync(timeout: _readingTimeoutMS);
                                DateTimeOffset readingEnd = DateTimeOffset.UtcNow;

                                if (newReading != null)
                                {                                   
                                    // create copy of new position to keep return references separate, since the same Position object is returned multiple times when a change listener is attached.
                                    _reading = new Position(newReading);                                   

                                    SensusServiceHelper.Get().Logger.Log("GPS reading obtained in " + (readingEnd - readingStart).TotalSeconds + " seconds:  " + _reading.Latitude + " " + _reading.Longitude, LoggingLevel.Verbose, GetType());
                                }
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("GPS reading failed:  " + ex.Message, LoggingLevel.Normal, GetType());
                                _reading = null;
                            }

                            _readingWait.Set();  // tell anyone waiting on the shared reading that it is ready
                            _readingIsComing = false;  // direct any future calls to this method to get their own reading

                        }).Start();
                }
            }

            _readingWait.WaitOne(_readingTimeoutMS);

            if (_reading == null)
                SensusServiceHelper.Get().Logger.Log("GPS reading is null.", LoggingLevel.Normal, GetType());

            return _reading;
        }
    }
}