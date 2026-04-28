# Coding Adventure: Procedural Moons and Planets

Source: https://www.youtube.com/watch?v=lctXaT9pxA0  
Creator: Sebastian Lague  
Published: 2020-07-11  
Duration: 22:47  
Transcript source: user-provided transcript pasted into Codex on 2026-04-28

## Summary

The video documents a Unity procedural generation experiment for miniature moons and planets. It starts with choosing a sphere mesh topology, then builds moon craters with compute shaders, adds procedural noise, applies triplanar texture/normal mapping, creates planet terrain with oceans and mountains, and finally tests the generated bodies in a small explorable solar system.

The most relevant ideas for Vortex are:

- Use sphere topology with evenly distributed vertices when deforming celestial bodies.
- Generate moon crater displacement from composable shape functions: cavity, rim, and flat floor.
- Blend crater components with smooth min/max style functions to avoid harsh transitions.
- Bias crater radius distribution so small craters are more common than large ones.
- Add separate noise layers for macro shape, ridges, detail, and warping.
- Use triplanar mapping to avoid UV unwrap pain on spherical worlds.
- Drive terrain color/material choices from height, slope, biome noise, and surface normal.
- Keep planet/moon generation adjustable and randomizable through exposed settings.

## Chapters

- 0:00 - Intro
- 0:25 - Spheres
- 3:09 - Craters
- 8:10 - Noise
- 10:40 - Triplanar Mapping
- 14:21 - Planet Shape
- 15:28 - Ocean
- 17:30 - Planet Shading
- 20:06 - Solar System

## Vortex Implementation Notes

### Sphere Topology

The video compares several approaches:

- Latitude/longitude sphere: easy to generate, but detail clusters at poles.
- Fibonacci sphere: excellent point distribution, but triangulation is less direct.
- Cube sphere: good for chunked LOD, but seams and lighting need careful handling.
- Icosphere: clean triangular base, but subdivision grows vertex count quickly.
- Modified icosphere-like approach: more direct control over points per triangle edge.

For Vortex, marching cubes currently avoids explicit sphere triangulation for generated bodies, but the same principle still matters: sampling, normals, LOD, and crater placement should avoid visible polar or random clustering artifacts.

### Craters

The crater model in the video is built from:

- A cavity/parabola.
- A raised rim.
- A flat floor.
- Smooth blending between these components.

Important tuning lessons:

- Crater floors should not be conic.
- Raised rims should be visible but not too sharp.
- Smaller craters should be much more common than larger craters.
- Large crater counts require distribution control; otherwise overlap becomes visually noisy.
- Per-crater variation in radius, floor height, and crater profile helps avoid repetition.

These points map directly to our `VoxelDataGenerator.compute` crater profile, where we now want:

- Even crater center distribution.
- Soft rim width/height.
- Flat or gently depressed crater floor.
- Smooth wall transition.
- Radius reduction when crater count is high.

### Noise

The video layers procedural noise in several ways:

- Basic smooth value noise.
- Fractal noise by combining octaves.
- Warped noise by offsetting sample positions with another noise field.
- Ridge noise using `1 - abs(noise)` style shaping.

For Vortex, this supports keeping separate noise fields for:

- Moon base shape.
- Moon ridges.
- Moon detail.
- Planet continents.
- Planet mountains.
- Planet biome/detail masks.

### Triplanar Mapping

Triplanar mapping samples a texture from three planar projections and blends them by surface normal. It avoids UV unwrapping and works well for spherical procedural bodies.

Important points:

- Projection scale must be controllable.
- Blend sharpness affects muddiness.
- Normal maps can be triplanar mapped too.
- Multiple normal/detail textures can be blended with noise to hide repetition.

For Vortex, triplanar scale should be normalized by body radius so a scale value behaves consistently across small and large moons.

### Planet Shape

The planet terrain section uses:

- Fractal noise for landmass shapes.
- Lower-value smoothing for ocean beds.
- Ocean depth multiplier for below-sea terrain.
- Ridge noise for mountains.
- A separate mask noise to control mountain abundance.

For Vortex, this supports keeping `PlanetShapeConfig` modular instead of collapsing terrain generation into one noise value.

### Ocean

The video uses a post-process ocean approach based on camera depth and ray/sphere intersection. This allows underwater traversal and water color based on view depth.

For Vortex, this can become a later rendering feature:

- Sphere-based ocean shell.
- Depth-based shallow/deep color blend.
- Shoreline blending with original scene color.
- Specular and diffuse lighting.
- Optional triplanar wave normal maps.

### Planet Shading

The video moves away from simple elevation bands and uses:

- Elevation.
- Steepness/slope.
- Surface normal relative to local up.
- Noise variation.
- Rock/grass thresholds.

For Vortex, mesh payload data passed through colors/UV channels should continue carrying height, slope, biome noise, and detail masks into shaders.

## Linked Resources From Video Page

- Delaunay/Voronoi sphere reference: https://www.redblobgames.com/x/1842-delaunay-voronoi-sphere/
- Smooth minimum article: https://www.iquilezles.org/www/articles/smin/smin.htm
- Triplanar normal mapping: https://www.medium.com/@bgolus/normal-mapping-for-a-triplanar-shader-10bf39dca05a
- Sphere mesh approaches: https://www.medium.com/game-dev-daily/four-ways-to-create-a-mesh-for-a-sphere-d7956b825db4
- Bloom reference: https://www.github.com/keijiro/KinoBloom
- Texture source: https://www.textures.com

## Action Items For Vortex

- Keep crater floors flatter and rims softer than a simple radial cone profile.
- Use more even crater center distribution when crater count increases.
- Bias crater radii so small craters dominate.
- Reduce crater radius slightly as crater count grows.
- Normalize triplanar texture scale by planet/moon radius.
- Keep moon flat-surface texture visible on crater floors and low-slope terrain.
- Keep steep texture mostly for crater walls, cliffs, and high-slope features.
- Tune crater count, radius, and depth together; raising count alone creates crowding.
- Consider per-crater floor height and rim variation.
- Consider separate ejecta/ray metadata for young craters only.

## Full Transcript

- **[00:00:02]** hi everyone i was recently messing about with gravity simulations and ended up making a little solar system which you
- **[00:00:08]** could fly about and explore it's hard to get terribly excited about exploring a bunch of coloured spheres though so
- **[00:00:14]** today i'm embarking on a journey to generate some simple procedural moons and planets
- **[00:00:27]** i need a sphere to begin with so this bit of code is one way of calculating the points and
- **[00:00:32]** it allows me to control the number of vertical and horizontal segments which means i can control how detailed the
- **[00:00:38]** planet will be unfortunately a lot of that detail is clumped around the poles so this probably isn't the right
- **[00:00:44]** sphere for me but plenty of fish in the sea for example if you've seen this video of mine on
- **[00:00:49]** boyd's you may recall me getting wildly sidetracked by something called the fibonacci sphere this is a really cool method
- **[00:00:56]** which allows us to choose exactly how many points we want and they're really evenly spread out as well i
- **[00:01:02]** had no idea though how to turn these points into triangles to create a mesh because they aren't calculated in
- **[00:01:07]** a neat winding order like the previous method but i found this interesting post which explains that you can project
- **[00:01:13]** the points onto an infinite plane triangulate those and then wrap it back up into a sphere i thought that
- **[00:01:19]** was really clever but i wouldn't mind something a little more straightforward so i'm going to keep exploring my options
- **[00:01:26]** six planes can be arranged to form a cube and if we then force the points to all be the
- **[00:01:30]** same distance from the center we get the humble cube sphere what's cool about this technique is that each face
- **[00:01:37]** can be divided into smaller subfaces which means you can render nearby regions of a planet in high detail but
- **[00:01:44]** save rendering time on more distant areas there's a lot of work i still need to do on this though
- **[00:01:49]** fixing the gaps and lighting issues that occur at the seams which seems like quite a headache so since my
- **[00:01:54]** goal for now is just simple miniature planets i'm going to have a look at another celebrity sphere the ico
- **[00:02:00]** sphere to create this one i learnt that you start with three rectangles with side lengths in the golden ratio
- **[00:02:07]** if you then join the points at the corners to form 20 triangles you have an icosahedron which is apparently
- **[00:02:13]** ancient greek for 20 seat in case you were wondering in any case each triangle can then be divided into
- **[00:02:19]** four sub-triangles and those new points projected onto the sphere we can repeat that as many times as we want
- **[00:02:25]** to increase the resolution the only trouble is the number of vertices grows really rapidly with each division so you
- **[00:02:32]** don't have such fine control at this point i came across an article about different types of spheres and in
- **[00:02:38]** the comments was ben golis describing a different approach where you simply add however many points you want along the
- **[00:02:44]** edges of each triangle and connect them up like so i spent a while figuring out all the index math
- **[00:02:49]** for this on paper which worked out great for me as you can see but i swapped some numbers around
- **[00:02:55]** and eventually ended up with something where i have decent control over the number of points and a pretty good
- **[00:03:00]** distribution as well here's my code for this which is probably many times longer and more convoluted than it needs
- **[00:03:06]** to be so nothing unusual there what i'd like to create first from this sphere is a little moon with
- **[00:03:12]** a surface covered in craters and to do that i'll need to calculate a height for each vertex i have
- **[00:03:19]** quite a lot of vertices so i'm going to do this in a compute shader so that i can process
- **[00:03:23]** loads of them in parallel on the gpu compute shaders can be unbelievably fast but they also have this distressing
- **[00:03:29]** habit of finding new and creative ways of crashing my computer so it's a bit of a love-hate relationship anyway
- **[00:03:36]** i'll get the current vertex position and then write a value to my height buffer based on that so as
- **[00:03:41]** a little test run for example i'll try 1 plus the sine of the position on the y axis multiplied
- **[00:03:47]** by some test value this will then be used elsewhere in the code to actually modify the height of that
- **[00:03:52]** vertex so i'll open up my shape settings here and if i increase that test value we can see the
- **[00:03:57]** sphere wobbling along with the sine wave so with my basic setup working i've begun looking at some craters i
- **[00:04:04]** basically just need to create a little bowl-shaped cavity with a raised rim around it and also a lot of
- **[00:04:10]** craters seem to have flat floors formed from shattered and melted rock larger craters also undergo some fascinating transformations for
- **[00:04:18]** example gravity causes the walls to slump down into step-like formations and somehow the ground is also pushed up to
- **[00:04:24]** form these giant central peaks when some cases rings of peaks i then fell down a bit of a rabbit
- **[00:04:31]** hole reading about the man who spent 27 years mining for the wealth of iron meteorite he figured it caused
- **[00:04:37]** this crater only to then learn it had been vaporized in the impact more distressingly i came across the so-called
- **[00:04:44]** mimas moon in orbit around saturn wake up sheeple that's no moon i should probably get back to work
- **[00:04:57]** so i need to try come up with some equations to describe the basic shapes of a crater for the
- **[00:05:03]** cavity for example i can just use a simple parabola like this to form the rim i experimented with a
- **[00:05:10]** bunch of things but what i ended up with was another little parabola mirrored around the x-axis which can be
- **[00:05:16]** shifted horizontally with the room width parameter and contracted or expanded using the rim steepness parameter then for the flow
- **[00:05:26]** of the crater i'll just have a straight line which i can shift up and down
- **[00:05:32]** now to combine these three equations together i'll make a crater shape function and this will first take the minimum
- **[00:05:38]** value between the cavity and the room resulting in this and then take the maximum value between that and the
- **[00:05:44]** floor which gives us the overall shape the lack of transition between the functions makes the shape really harsh and
- **[00:05:51]** unnatural though so several episodes ago back when i was experimenting with ray marching i learnt about the smooth min
- **[00:05:58]** function for blending shapes together this function just takes whichever is smallest between a and b but with some smoothing
- **[00:06:05]** applied controlled by the parameter k to make this return the max instead of the min you can just make
- **[00:06:10]** k negative by the way so if i use that for my craters and increase the smoothing parameter you can
- **[00:06:16]** see it looks a lot more natural
- **[00:06:22]** i now just need to translate that to my moon height compute shader and behind the scenes i'm also now
- **[00:06:26]** feeding it a buffer containing a random position and radius for each crater plus all the various settings it needs
- **[00:06:33]** then going into the scene i'll increase the maximum radius and we can see a single crater appear let me
- **[00:06:40]** add in a bunch more of these and to try to get them looking a bit more crater-like i'll mess
- **[00:06:44]** about with the room width and steepness parameters and then also the smoothing amount to make the shapes a little
- **[00:06:50]** less harsh
- **[00:06:56]** that's looking okay so i'll add in a bunch more of them it looks terrible now but i think would
- **[00:07:03]** help is if smaller craters were a lot more common than larger ones so i'm generating a random number between
- **[00:07:09]** 0 and 1 to determine the size of each crater and if we pass that value into this function that
- **[00:07:14]** i'm graphing here we'll just get out the exact same thing we put in because y is equal to x
- **[00:07:20]** at every point so that's pointless but here's the code for this function and as you can see it's controlled
- **[00:07:26]** by a parameter called bias the bias is currently zero but as i increase it towards one you can see
- **[00:07:33]** how the curve changes so now most values we input to the function will just get a small output and
- **[00:07:39]** only a few will result in a relatively large output back to the moon i can now use this little
- **[00:07:45]** slider to change the distribution of the crater sizes and make small craters more common than larger ones the last
- **[00:07:52]** parameter to play around with is the floor height and currently this affects all of the craters but i'm going
- **[00:07:58]** to make a little change to have the floor height determined per crater just to add some variation all right
- **[00:08:04]** that was very long-winded but we now have some fairly cute little craters the moon is exceedingly smooth at the
- **[00:08:12]** moment though so i want to add some little bumps and ridges using noise the noise function i'm using just
- **[00:08:18]** takes in a point in space and gives back a value between negative one and one which changes smoothly as
- **[00:08:23]** you move the point around the code for that noise function which is not something i wrote myself looks like
- **[00:08:29]** this very confusing anyway we can make multiple layers of increasing detail by spacing the sample points further and further
- **[00:08:38]** apart each time if we combine all these layers into one well it's a bit of a mess but if
- **[00:08:44]** we make it so that each layer contributes less and less to the overall result we get some nice structured
- **[00:08:49]** and detailed noise out of it the code for that looks like this we can do some fun things with
- **[00:08:54]** it for example we're sampling the noise function at a point in space so what if we offset that point
- **[00:09:00]** using more noise well as it turns out this warps the noise in pretty cool ways if you add a
- **[00:09:06]** bit of color and animate some values you can get some pretty stunning results
- **[00:09:18]** modifying it another way by taking 1 minus the absolute value of the noise results in a more ridge-like appearance
- **[00:09:24]** useful for well ridges and mountains i had some fun playing around with different colors and just watching the noise
- **[00:09:31]** scroll by
- **[00:09:38]** just for fun let's try use this to create something similar to hyperion one of saturn's less threatening lumpy spongy
- **[00:09:45]** looking moons so i have some different noise settings over here like the ridge noise i was just talking about
- **[00:09:51]** this has a bunch of parameters i can play with like the scale and the power looks great we can
- **[00:10:00]** also subtract the ridges instead of adding them which i think looks kind of interesting anyway to get a kind
- **[00:10:06]** of lumpy shape like hyperion i'll add some simple noise and scale the frequency way down i can then offset
- **[00:10:12]** the sample position until i get a shape that i like i'll then also add some higher frequency noise for
- **[00:10:19]** a bit of detail
- **[00:10:22]** hyperion had a lot of holes punched in it so i'll put the number of craters way up to something
- **[00:10:27]** like 3000 and it's quite amazing that my terrible crater code which loops over every single crater on every single
- **[00:10:33]** vertex is actually still running reasonably quickly anyway here's my little rock if i want to add more detail without
- **[00:10:43]** increasing the number of vertices i'll need to use textures so typically someone creates a 3d model and then painstakingly
- **[00:10:51]** adds seams to indicate how it can be unwrapped into two dimensions a talented artist then draws details on top
- **[00:10:57]** of that and we get our beautiful result to bypass the annoying unwrapping step i'm going to try use a
- **[00:11:04]** technique called tri-planar mapping and the idea is to just use the position of the model's vertices to read from
- **[00:11:10]** the texture of course the position is in three dimensions and the texture in two so over here i've just
- **[00:11:16]** picked the x and y axes of the position to sample from the texture this gives great results when we're
- **[00:11:24]** looking into the x y plane but from other directions it's somewhat less convincing so the idea continues we sample
- **[00:11:30]** the texture two more times using the zy and xz axes now we just need to blend between these three
- **[00:11:36]** different textures to favor whichever one aligns best with the surface which we can do based on the surface normal
- **[00:11:43]** we can also raise the normal to some power to affect the sharpness of the blending and multiply the position
- **[00:11:48]** by some factor to control the scale of the texture if we look at the sphere now you can see
- **[00:11:54]** it's blending between the projections but the result is kind of muddy and that's what the sharpness parameter was for
- **[00:12:00]** i can fiddle with that and get it looking how i want also if i change the scale parameter surprise
- **[00:12:05]** surprise it scales the texture i'll grab one of those textures from my warp noise experiments to use instead this
- **[00:12:12]** needs some lighting so i'll quickly go back into the shader and calculate the dot product of the surface normal
- **[00:12:18]** and the direction towards the light source and then multiply the output color by that this looks a bit bland
- **[00:12:25]** though so i'd like to spice things up with a normal map i did some research on how to get
- **[00:12:30]** that to work with tri-planar mapping and once again ben was there to guide the way with this very detailed
- **[00:12:36]** article on the subject so here's the triplina code now modified to work with normal maps in my fragment shader
- **[00:12:43]** i can then get the normal from the normal map and use that to calculate the lighting i found these
- **[00:12:50]** two handsome maps online so let's try them out immediately the surface looks a lot more interesting in detail than
- **[00:12:57]** it really is here's what the other map looks like
- **[00:13:04]** so i'll switch over to my actual moon surface and raise the strength of the normal mapping here i'm actually
- **[00:13:10]** using both normal maps and blending between them based on some noise to try break up the repetition a bit
- **[00:13:17]** what i'd like to do next is add some color variation so i've generated some noise which is stored in
- **[00:13:22]** the mesh uvs and using that i can split the moon into two color regions i also have some settings
- **[00:13:28]** for warping that noise because i like warping everything now
- **[00:13:36]** one little detail i wanted to add is these cool looking rays of ejected material you can see around relatively
- **[00:13:41]** young craters before exposure to space darkens them and they fade into the background to do this i randomly pick
- **[00:13:48]** a couple of craters to have this feature and i then did some slightly suspicious maths to calculate uv coordinates
- **[00:13:54]** around them like so and it can then sample from a texture to draw the rays this doesn't look great
- **[00:14:00]** to be honest and it only works for a small number of creators but it's better than nothing for now
- **[00:14:05]** i think
- **[00:14:15]** there's clearly still a lot of room for improvement of the moons but i'd like to move on for now
- **[00:14:19]** and begin creating some little planets so i'll write a little shader that just uses the fractal noise from earlier
- **[00:14:25]** to create some landmass shapes i'll smooth out the lower values to create an ocean bed and then i'll multiply
- **[00:14:31]** all heights below zero by an ocean depth parameter i'll then also add in some ridge noise for mountains and
- **[00:14:37]** finally add those noises together and write it to the height buffer so here's the noise in action i'll deepen
- **[00:14:47]** the oceans and flatten the floor a little and then i'll go into the ridge noise settings and push up
- **[00:14:52]** some mountains the only trouble is the mountains have kind of taken over and i'd like to retain some flat
- **[00:14:59]** areas so i'll add another layer of noise to act as a mask and i'll multiply the mountain noise by
- **[00:15:05]** that mask so now if i shift the mask around i have some control over how abundant the mountains are
- **[00:15:14]** here's what the mask actually looks like by the way now i'm not sure what the best way to handle
- **[00:15:20]** the oceans is because i want the player to be able to go underwater what i'm going to try for
- **[00:15:25]** now is doing it as a post-processing effect so after everything else has been drawn this shader will be run
- **[00:15:30]** for every pixel on the screen and in here we can access the camera's depth texture to figure out how
- **[00:15:36]** far away everything is otherwise the ocean would just be drawn on top of everything i then collaborated with stack
- **[00:15:43]** overflow to write this little ray sphere intersection function which can tell me the distance to the surface of a
- **[00:15:48]** sphere and also the distance it travels through it to the other side in my ocean shader i'll then use
- **[00:15:54]** that function to figure out how much water we're actually looking through for the current pixel if it's more than
- **[00:16:00]** nothing i'll draw white otherwise i'll just leave the original color that gives me this to add some color i'll
- **[00:16:08]** go back to the shader and use the depth of the water to blend between a shallow and deep color
- **[00:16:13]** and also blend in some of the original color in shallow regions to give a soft transition at the shore
- **[00:16:20]** so i can control the transparency with this parameter and the blending of the shallow and deep colors with this
- **[00:16:26]** one i can also obviously change the colors
- **[00:16:32]** now i'd like to be able to see the sun in the water so i looked up the equations for
- **[00:16:35]** specular highlights and decided to go with the gaussian model because apparently it's slightly better citation needed the calculation is
- **[00:16:43]** kind of expensive though primarily because of this arc cosine function which i hear as a big no-no in shaders
- **[00:16:49]** so i might replace this with a cheaper model later i'll then also add some diffuse shading and apply this
- **[00:16:55]** lighting to the ocean color so i can now use the smoothness slider to control the size of the highlight
- **[00:17:02]** and behind the scenes i also added in some wave-like normal maps using that same triplanar technique from earlier and
- **[00:17:07]** that gives a kind of nice effect it would of course be a lot nicer to have actual waves rising
- **[00:17:13]** and falling but i have no clue how to do that especially using this technique on a sphere something to
- **[00:17:18]** look into for sure now with the way i've done this it is possible to go underwater but the effect
- **[00:17:23]** is quite underwhelming at the moment still i'm going to call it good enough for now and move on to
- **[00:17:28]** coloring the terrain in one of my old tutorial series i tackled this problem by assigning colors based on elevation
- **[00:17:35]** but this results in kind of silly looking bands of color i've learnt at least one new trick since then
- **[00:17:40]** though which is to consider the steepness of the terrain calculated as the dot product between the surface normal and
- **[00:17:46]** the up direction at that point we can then make grass for example appear only where the steepness is below
- **[00:17:52]** a certain threshold i do still use the elevation to not allow grass to grow too high up and also
- **[00:17:58]** to add some slight color variation something i've been struggling with a lot is the look of the mountains because
- **[00:18:05]** they look too smooth i tried adding in a rocky normal map but it looked a little bit horrendous in
- **[00:18:12]** an earlier iteration i experimented with flat shading and also adding a threshold so that only steep regions would be
- **[00:18:18]** flat shaded but i wasn't so happy with this either ideally i'd like to use a technique i experimented with
- **[00:18:24]** a while back where you simulate hundreds of thousands of droplets of water to try and mimic the effects of
- **[00:18:29]** erosion just thinking about the changes i'd have to make for this to run on a spherical world seems like
- **[00:18:34]** it'd become ridiculously slow though so my latest attempt has been this kind of cartoony effect with solid bands of
- **[00:18:40]** color to try emphasize the rockiness of it
- **[00:18:46]** i'm not sure if that's the style i want to go for or not but i've been experimenting a bit
- **[00:18:49]** adding for example this sort of speckled effect to the grass up close you can see the terrain looks a
- **[00:18:55]** bit raggedy so i do think it would help if i could increase the resolution some more i have several
- **[00:19:00]** levels of detail that i can switch between but that's just for the entire planet so i think i might
- **[00:19:05]** need to break it up into chunks after all headaches await me i guess anyway we can of course randomize
- **[00:19:13]** the planet shape which just works by changing where in space the noise values are sampled from and we can
- **[00:19:18]** randomize the colors although the combinations can sometimes be a bit of an eyesore i've then also been having some
- **[00:19:24]** fun generating other little planets and moons to fill up my test solar system for example here's one that i've
- **[00:19:30]** nicknamed cyclops because of its one giant crater i quite enjoy just messing about with the settings and seeing what
- **[00:19:37]** happens as you can probably tell there's a lot of warping going on in this one so i'll play with
- **[00:19:42]** that parameter a bit as well cyclops has a little moon based on those hyperion experiments from earlier there are
- **[00:19:49]** a bunch more but i'm quickly going to hop over to blender and try whip up a blocky little astronaut
- **[00:19:53]** so we can go explore them properly
- **[00:19:59]** i'll get this imported into the game make a build and oh no what have i done this time
- **[00:20:08]** well that was weird but here we are by the home planet and alongside it is its little moon i'm
- **[00:20:13]** going to see if i can zip over that
- **[00:20:18]** quickly
- **[00:20:46]** well let's go see what's out there looks like a whole lot of
- **[00:21:00]** nothing
- **[00:21:06]** i'll head back to the ship because there's not actually much to see here but maybe several episodes down the
- **[00:21:11]** line there'll be some little knife forms to go cat or something for now though i'll just hop over to
- **[00:21:16]** the next door
- **[00:21:28]** planet
- **[00:21:32]** oh good thing i haven't added a damage system yet with the way i pilot this
- **[00:21:42]** thing
- **[00:21:56]** so obviously things are kind of wonky looking at the moment but if i can work on the graphics a
- **[00:22:00]** bit and add in some trees and bushes and maybe some little life forms as i mentioned earlier i think
- **[00:22:06]** it could potentially be a nice little place to wonder about for my last stop on today's journey i'll fly
- **[00:22:15]** over to the twin planets which orbit up close and personal with the sun and i'll go pay a little
- **[00:22:20]** visit to the fire twin that's going to be all from me for today obviously there's loads of things i
- **[00:22:26]** could do to improve the look of the moons and planets i just wish i knew what they were and
- **[00:22:31]** how to do them but that just means there's loads left to learn so i'd better get to it as
- **[00:22:36]** always i hope you've enjoyed watching and until next time
- **[00:22:45]** cheers
