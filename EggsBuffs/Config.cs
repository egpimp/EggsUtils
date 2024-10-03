using BepInEx.Configuration;
using EggsUtils.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EggsUtils.Config
{
    public static class Config
    {
        /* Realistically, this shouldn't be needed since sharing mod profiles also shares the configs, but just because
         * humans are indeed humans and not everyone uses mod managers (They should) and also because I don't wanna
         * completely delete the time I put in this I'm keeping it for now, will probably remove later though
         */

        //Makes the config code from all of the config values
        public static string PrepareConfigCode(ConfigFile file)
        {
            //Start with blank string, this will slowly become the config code
            string configCode = string.Empty;
            //How many default values we find in a row, this helps us to simply skip over sections where all values are default
            int defaultCounter = 0;
            //For every value in the file...
            foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> config in file)
            {
                //This just is easier reference for us
                ConfigEntryBase value = config.Value;
                //Start with another blank string
                string tempValue = string.Empty;
                //This is the 'type', since we can have bools, floats, ints, and we need to know which one so we convert it properly
                string tempType;
                //If the value is just the default value...
                if (value.BoxedValue.Equals(value.DefaultValue))
                {
                    //Note it
                    defaultCounter += 1;
                    //This just exits if the last value was a default safely, basically prevents things from exploding
                    if (file.Last().Equals(config)) tempType = defaultCounter.ToString();
                    //We do this so nothing is added until AFTER all the defaults are found and marked
                    else tempType = string.Empty;
                }
                //Otherwise if the value is NOT the default value...
                else
                {
                    //If there are any default values queue'd up to be marked...
                    if (defaultCounter > 0)
                    {
                        //Put the number where it goes
                        configCode += defaultCounter.ToString();
                        //Reset the counter
                        defaultCounter = 0;
                    }

                    //This gets us b if bool, s if short (float), i if int, and u if uint
                    tempType = value.BoxedValue.GetType().Name[0].ToString().ToLower();

                    //If it is a bool, the string section will be b1 or b0 for true or false
                    if (tempType == "b") tempValue = (bool)value.BoxedValue ? "1" : "0";

                    //If it is a float...
                    else if (tempType == "s")
                    {
                        //Grab the whole float first off
                        float floatValue = (float)value.BoxedValue;
                        //If it's too high...
                        if (floatValue > 3843)
                        {
                            //Tell them it too high and set to default
                            Log.LogWarning("Value of config field : " + value.Definition.Key + " too high, resetting to default)");
                            value.BoxedValue = value.DefaultValue;
                            //Then perform the usual default stuff so we brick things
                            defaultCounter += 1;
                            if (file.Last().Equals(config)) tempType = defaultCounter.ToString();
                            else tempType = string.Empty;
                        }
                        //If it too low...
                        else if (floatValue < 0)
                        {
                            //Tell them too low and set to default
                            Log.LogWarning("Value of config field : " + value.Definition.Key + " below zero, resetting to default)");
                            value.BoxedValue = value.DefaultValue;
                            //Then perform the usual default stuff so we brick things
                            defaultCounter += 1;
                            if (file.Last().Equals(config)) tempType = defaultCounter.ToString();
                            else tempType = string.Empty;
                        }
                        //Otherwise we can move onto encoding it normally
                        else
                        {
                            //Grab the whole number
                            int whole = Convert.ToInt32(System.Math.Floor(floatValue));
                            //Set the value to the whole number as a base62 2 digit value
                            tempValue = Conversions.ToBase62(whole);
                            //Now we get the decimals in the back, as a 2 digit int
                            int dec = Convert.ToInt32((floatValue - whole) * 100f);
                            //Fix boxed value so it's not painfully precise anymore
                            value.BoxedValue = (float)whole + Convert.ToSingle(System.Math.Round(dec / 100f, 2));
                            //Convert the decimal part to base62 also and pass it along
                            tempValue += Conversions.ToBase62(dec);
                        }
                    }
                    //Otherwise if it is an int...
                    else if (tempType == "i")
                    {
                        //intvalue is just the value of the int duh
                        int intValue = (int)value.BoxedValue;
                        //If too big tell them and default it
                        if (intValue > 3843)
                        {
                            Log.LogWarning("Value of config field : " + value.Definition.Key + " too high, resetting to default)");
                            value.BoxedValue = value.DefaultValue;
                            //Then perform the usual default stuff so we brick things
                            defaultCounter += 1;
                            if (file.Last().Equals(config)) tempType = defaultCounter.ToString();
                            else tempType = string.Empty;
                        }
                        //If too small tell them and default it
                        else if (intValue < 0)
                        {
                            Log.LogWarning("Value of config field : " + value.Definition.Key + " below zero, resetting to default)");
                            value.BoxedValue = value.DefaultValue;
                            //Then perform the usual default stuff so we brick things
                            defaultCounter += 1;
                            if (file.Last().Equals(config)) tempType = defaultCounter.ToString();
                            else tempType = string.Empty;
                        }
                        //If value is fine set the value to it
                        else tempValue = Conversions.ToBase62(intValue);
                    }
                    //Otherwise if it is a uint...
                    else if (tempType == "u")
                    {
                        //Grab uint value
                        uint uintValue = (uint)value.BoxedValue;
                        //If too big default and say it
                        if (uintValue > 3843)
                        {
                            Log.LogWarning("Value of config field : " + value.Definition.Key + " too high, resetting to default)");
                            value.BoxedValue = value.DefaultValue;
                            //Then perform the usual default stuff so we brick things
                            defaultCounter += 1;
                            if (file.Last().Equals(config)) tempType = defaultCounter.ToString();
                            else tempType = string.Empty;
                        }
                        //If too small default and say it
                        else if (uintValue < 0)
                        {
                            Log.LogWarning("Value of config field : " + value.Definition.Key + " below zero, resetting to default)");
                            value.BoxedValue = value.DefaultValue;
                            //Then perform the usual default stuff so we brick things
                            defaultCounter += 1;
                            if (file.Last().Equals(config)) tempType = defaultCounter.ToString();
                            else tempType = string.Empty;
                        }
                        //If it works then put it in value
                        else tempValue = Conversions.ToBase62(Convert.ToInt32(uintValue));
                    }
                }
                //Finally add the type + value to the code, loop until end and boom done
                configCode += tempType + tempValue;
            }
            //Reload the file for safety
            file.Reload();
            //Return the code
            return configCode;
        }

        //Turns string code into actual values, AKA pure agony
        public static void LoadConfigCode(string code, ref ConfigFile file)
        {
            //This is the length of the code
            int length = code.Length;
            //Pointer tells us where we are at
            int pointer = 0;
            //Section helps us identify where tf we at in the code
            int section = 0;
            //How many defaults in a row we hittin
            int numDefaults = 0;
            //For every config value, should be in same order as person who did the getconfig
            foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> config in file)
            {
                //Helps us reference it easier
                ConfigEntryBase configEntry = config.Value;
                //Pointed is the character we are pointed at rn
                char pointed = code[pointer];
                //Num helps us identify default values
                int num;
                //If we are dangerously close to indexoutofbounds this will helps us not be that
                bool isLast = false;
                //If we haven't hit any defaults yet, check for them
                if (numDefaults == 0)
                {
                    //Keep scanning through basically until we are no longer viewing a number, so we can read off any length of number of defaults
                    while (int.TryParse(code.Substring(pointer, section + 1), out num))
                    {
                        //Section lets us look further each loop until we hit a nothing
                        section += 1;
                        //Numdefaults is just the num so we know how many defaults we just hit
                        numDefaults = num;
                        //If we are boutta go out of bounds say so and gtfo before we die
                        if (pointer + section >= code.Length)
                        {
                            isLast = true;
                            break;
                        }
                    }
                    //If we did hit any defaults...
                    if (section > 0)
                    {
                        //Mark the pointer for that length so we know how far to be for next config value
                        pointer += section - (isLast ? 1 : 0);
                        //Reset the section
                        section = 0;
                    }
                }
                //If we DID encounter defaults last step...
                if (numDefaults > 0)
                {
                    //Count down the defaults we have handled
                    numDefaults -= 1;
                    //Set the value to default
                    configEntry.BoxedValue = configEntry.DefaultValue;
                    //We ain't gotta do shit else so skip the rest of the logic
                    continue;
                }
                //Push pointer up by one beforehand so it is ready to read off the value
                pointer += 1;
                //If the string is saying we expect a bool...
                if (pointed == Convert.ToChar("b"))
                {
                    //Bools are only 1 digit values, so we read off a length of 1 only
                    section = 1;
                    //Set the value to true if 1, false if 0, simple
                    configEntry.BoxedValue = code.Substring(pointer, section) == "1";
                }
                //If the string says we expect a float (short)...
                else if (pointed == Convert.ToChar("s"))
                {
                    //We will be reading off 2 numbers at a time, once for the whole part and again for the decimal
                    section = 2;
                    //Whole is just the first two parts of the value converted back to an int
                    int whole = Conversions.FromBase62(code.Substring(pointer, section));
                    //Dec is just the second to parts of the value converted to an int
                    int dec = Conversions.FromBase62(code.Substring(pointer + section, section));
                    //Put the two pieces together and make it an actual float
                    float convertedValue = whole + (dec / 100f);
                    //The finished value is put into the converted value part
                    configEntry.BoxedValue = convertedValue;
                    //Section +2 so we know that we skip ahead by 4 total spots in the string
                    section += 2;
                }
                //If the string says we expect an int...
                else if (pointed == Convert.ToChar("i"))
                {
                    //Only a 2 digit value to read and skip off of
                    section = 2;
                    //Set it straight to the value, simple no problem
                    configEntry.BoxedValue = Conversions.FromBase62(code.Substring(pointer, section));
                }
                //If the string says we expect a uint...
                else if (pointed == Convert.ToChar("u"))
                {
                    //Again it's just a 2 digit num to read off
                    section = 2;
                    //Tempval is cause it is slightly more painful than int to do all in a single line
                    uint tempVal = (uint)Conversions.FromBase62(code.Substring(pointer, section));
                    //Then set value to the one we found out
                    configEntry.BoxedValue = tempVal;
                }
                //If somehow the string has no idea what is going on...
                else
                {
                    //Cancel the whole reading and note it in console
                    Log.LogError("Invalid code section, process aborted");
                    return;
                }
                //Move the pointer up the necessary amount to read the next value
                pointer += section;
                //Reset section
                section = 0;
            }
            //Tell them the config code went through just fine
            Log.LogMessage("Config code loaded, restart game for it to take effect");
            //Save the file
            file.Save();
            //Reload the file
            file.Reload();
        }
    }
}
