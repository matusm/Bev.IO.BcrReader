BcrReader
=========

Library for reading GPS raster and profile data files according to ISO 25178-7, ISO 25178-71 and EUNA 15178. 

## Overview
This class can be used by applications which need to read raster data or single profil files in the popular BCR format.
All dimensional values  used by this class are in units of meter.

### Constructor

* `BcrReader(string filename)`
Creates a new instance of this class taking a file name as the single argument. Takes care of reading and interpreting the file.

### Methods

* `double[] GetProfileFor(int profileIndex)`
  Gets a single profile out of the raster data. 
  
* `Point3D[] GetPointsProfileFor(int profileIndex)`
Gets the cartesian coordinates of all points making the profile. Usually one is interested in the X and Z coordinates only.
  
* `double GetValueFor(int pointIndex, int profileIndex)`
Gets the single hight value for the given indices.

* `Point3D GetPointFor(int pointIndex, int profileIndex)`
Gets the cartesian coordinates of the point for the given indices. The coordinates are returned in an `Point3D` object.

### Properties

* `Status`
Gets the error status as an element of the `ErrorCode` enumeration. The object members should be used only when this property is `ErrorCode.OK`.

* `VersionField`
Gets the file type and version as defined in the standards.

* `CreateDate`
Gets the date of the creation of the raster data.

* `ModDate`
Gets the date of the modification of the raster data.

* `ManufacID`
Gets the instrument manufacturer information.

* `NumPoints`
Gets the number of points per profile.

* `NumProfiles`
Gets the number of profiles of the raster data.

* `XScale`
Gets the spacing between two consecutive points in a profile.

* `YScale`
Gets the spacing between two consecutive profiles. This value is 0 for single profile files.

* `ZScale`
Gets the scale of hight values in the original file. This is for information only since values from this class method's are already properly transformed.

* `MetaData`
Gets a dictionary of all metadata found in the original file. 

* `RawMetaData`
Gets a list of all metadata found in the original file. 


## Known problems and restrictions
Some legacy parameters like "Compression" and "CheckType" are not evaluated. They were anicipated in the EUNA 15178 document. The current ISO standards call to include them in the file but using default values only. We are not aware of any legacy software or data files using "Compression" or "CheckType".

Trailer formatted according to ISO 25178-71 (a pseudo-XML format) can not be interpreted (yet).

The recommended file name extension for the BCR format is `.sdf`. The library does not default to this extension, the user is responsible to provide the full file name to the constructor.
