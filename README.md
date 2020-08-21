# CSGO VMF Generator

This is a generator with the purpose of making an easy way to generate CS:GO maps or making it easier to generate things in the VMF format (With a focus on CS:GO)

This is a fairly basic library, and I'm open to pull requests for improvements. I've really only done lots of early setup, and it's missing a lot of potentially great features.
Currently it supports:
- Generating cubes, stairs, slopes, and any flat 2D polygon (concave or convex, it will automatically convert) that can be rotated.
- Wall generation, simply passing in another created brush and it will create walls around that shape.
- Primitive map generation from a black and white image. Examples shown below.
- Adding visgroups to generated brushes, with included [TAR](https://github.com/Terri00/CS-GO-Auto-Radar) visgroup names for easily generating radars.
- Texture support. UVs are a bit wonky, but you can easily select what textures you want for each brush.
- Entity support. Included manually are only a light environment, T and CT spawns, however it is fairly easy to add a new entity to EntityTemplates.cs

Examples of what can be made:

Input:

![Image of Input](https://github.com/7ark/CSGO-VMF-Generator/blob/master/ExampleImages/InputExample1.png)

Result:

![Image of Result](https://github.com/7ark/CSGO-VMF-Generator/blob/master/ExampleImages/Result1.png)

## How do I run this?
Well to run what I've provided by default, you simply run the EXE and it'll generate the basic stuff I've provided.
If your CS:GO folder isn't in the default C drive location I've entered (seen in the Program.cs), you'll need to pass it an argument with the path you want the VMF generated.
To do more you'll need to edit a little bit of code.

## Code Guide:
There are several key classes you'll need to know about to edit the code from a basic to advanced level. Starting at the basics, EasyInputLayer.cs is where you can easily change or 
add new shapes to generate. Here I've already setup examples, where I add GenerationMethods to a list and return that. Generation methods return a list of shapes (You can see them
and their types in GenerationMethods.cs). 

### Basic
Currently I have 3 basic ones, a Misc which is simply what I used for testing, it is making a simple shape and some stairs.
Then theres the HollowCube, which is self explanatory. I'm using that for the skybox in this example.
Then the last I have commented out for the moment. This is the image generation. If you uncomment it (I suggest commenting out the Misc if you do this or it'll get cluttered)
you will need to make sure to create an input folder in the same folder as the exe (in the default case this is the debug folder) if it doesnt already exist, and then you'll
add a black and white image such as the example above. This isn't perfect, but it does a pretty good job with a lot of shapes. There are still plenty of improvements to that entire algorithm that could be made though.

Then in the EasyInputLayer.cs theres also the entities below that. I am simply adding strings generated from the EntityTemplates.cs class, which simply fills out the format for
that particular entity, it should be fairly easy to add your own if you wish. I provide examples of adding a light environment and spawns.

### Advanced
If you wish to get more involved in the code, edit it to add more shapes, or make pull requests etc. then you'll end up getting deeper into the other classes.
The main classes to keep an eye on are GenerationMethod.cs and Shape.cs, these are where the base areas for creating new shapes and such are. Its mostly combining things to create
new shapes from the Polygon shape. For example slopes are create by making a flat Polygon in the shape of a slope and rotating it to the correct position. You can see an example
of me doing this in the StairGenerator for the clipping slope.

For deeper subjects theres a few things.
Image generation is handled in GenerationMethods at the Image generation method. Theres a lot going on here, and it will probably take a lot to absord. I am taking an image, adding
artifical lines to essentially make all of the white space one non-looping polygon. I do this because what I'm using to create polygons out of images isn't able to handle looping
images, it simply deletes the inner section. I am unable to write anything to do more with this, so I create extremely thin lines at key points to get around this.
All of that is handled in that one class, I've tried to comment the important bits.

If you want to make the triangulation better, cleaner, or even change it to quadrangulation (to make "better" looking brushes) you can find all of that in the PolygonTriangulator.cs.
Here I follow a [great tutorial](https://www.gamedev.net/tutorials/programming/graphics/polygon-triangulation-r3334/) to implement triangulation, and then add my own adjustments
to accomodate what the tool requires.

For anything else, it should all be fairly self explanatory. You'll need to look through the code and experiment.

## Pull Requests
I am open to any pull requests to improve the code base, adding new features, even if its just adding new Entity Templates, Textures, etc. 
This is not a project I plan to support heavily, it is something I update in my spare time to expand and improve.

## Personal Support
If you’re interested in supporting me personally, you can follow me on [Twitter](https://twitter.com/The7ark) or [Instagram](https://www.instagram.com/the7ark/). 
If you want to support me financially, I have a [Patreon](https://www.patreon.com/7ark) and a [Ko-fi](https://ko-fi.com/sevenark).
