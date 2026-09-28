using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeMaster.Core.Enums;

namespace TypeMaster.Services;

/// <summary>
/// 内置海量中英文题库（按练习类型 + 难度分级）。
/// 中文：小学1-6年级语文课文风格，每篇100-1000字。
/// 英文：小学3-6年级英语阅读风格，每篇100-1000词。
/// </summary>
public static class TextLibrary
{
    private static readonly Dictionary<Difficulty, List<string>> EnglishTexts = new()
    {
        [Difficulty.Easy] = new()
        {
            // Easy English: ~80-150 words, simple sentences, grades 3-4 level
            @"My name is Tom. I am ten years old. I live in a small house with my parents and my little sister. My favorite color is blue. I like to play football with my friends after school. On weekends, I often go to the park with my family. We fly kites and have picnics there. I also like reading storybooks before bed. My best friend is Jack. He sits next to me in class. We walk to school together every morning. School starts at eight o'clock and finishes at four o'clock. I have many subjects, but English and art are my favorites. I want to be a teacher when I grow up because I love helping others learn new things.",

            @"There is a beautiful garden behind my house. In spring, many flowers bloom in the garden. There are red roses, yellow sunflowers, and white lilies. Butterflies fly from one flower to another. Bees are busy collecting sweet honey. A small pond is in the middle of the garden. Goldfish swim happily in the clear water. My grandmother likes to sit by the pond in the afternoon. She tells me stories about the flowers and the birds. Sometimes I help her water the plants. The garden is my favorite place in the whole world.",

            @"Today is Sunday. The weather is sunny and warm. My mother takes me to the zoo. There are so many animals there! First we see the elephants. They are very big and have long noses. Then we watch the monkeys jumping from tree to tree. They are funny and clever. Next we visit the pandas. They look cute eating bamboo slowly. After that, we see lions, tigers, and giraffes too. The giraffes have very long necks. I buy some ice cream at the zoo shop. It tastes delicious. What a happy day!",

            @"I have a pet dog named Buddy. He is two years old. His fur is brown and white. He has big black eyes and a wagging tail. Buddy is very friendly and loves to play. Every morning, he waits for me at the door when I wake up. After school, he runs to greet me with excitement. I feed him twice a day and take him for walks in the evening. He can do many tricks like sitting, shaking hands, and rolling over. Buddy is not just a pet, he is my best friend. I love him very much.",

            @"Our school is big and beautiful. There is a large playground where students play during breaks. The library has thousands of books on many different subjects. My classroom is on the second floor. It has thirty desks and chairs. Our teacher Mrs. Wang is very kind and patient. She teaches us math and science. We also have music class, art class, and PE every week. I have many good friends at school. We study together, eat lunch together, and play games together. School is a happy place for me."
        },
        [Difficulty.Normal] = new()
        {
            // Normal English: ~200-400 words, varied sentences, grades 4-5 level
            @"The forest is home to countless plants and animals. Tall trees reach up toward the sky, their leaves catching sunlight and turning it into food through photosynthesis. Beneath the trees, smaller plants grow in the shade. Flowers of every color attract bees and butterflies. Birds build nests in the branches and sing beautiful songs. Squirrels run up and down the trunks collecting nuts for winter. In the quiet corners of the forest, you might spot a deer drinking from a stream or a rabbit hopping through the underbrush. Forests are important because they give animals a place to live. They also clean the air we breathe and provide wood and medicine for people. However, forests around the world are getting smaller because people cut down too many trees. We must protect our forests so that future generations can enjoy them too.",

            @"Last summer, my family went on a trip to the countryside. We stayed at my grandfather's farm for two weeks. Life on the farm was very different from life in the city. Every morning, I woke up to the sound of roosters crowing. My grandfather taught me how to collect fresh eggs from the chicken coop. The eggs were still warm! I also learned to feed the pigs and milk the cows. In the afternoon, we rode bicycles along the country roads. The air smelled of grass and wildflowers. At night, the sky was full of bright stars. I had never seen so many stars before. My grandfather told me the names of different constellations. That summer taught me where our food comes from and how hard farmers work. It was the most memorable vacation I have ever had.",

            @"Water is the most important substance on Earth. All living things need water to survive. Humans can live without food for several weeks, but only a few days without water. Our bodies are made up of about sixty percent water. We use water for drinking, cooking, washing, and growing crops. However, clean water is becoming scarce in many parts of the world. Some people have to walk miles every day just to find water to drink. Factories sometimes pollute rivers and lakes with chemicals. We can all help save water by taking shorter showers, fixing leaky taps, and not wasting water when we brush our teeth. Remember, every drop counts. Protecting water resources is everyone's responsibility.",

            @"The Great Wall of China is one of the most amazing structures ever built by humans. It stretches over thirteen thousand miles across northern China. Construction began more than two thousand years ago. Millions of workers carried heavy stones up steep mountains to build it. The wall was built to protect China from invaders coming from the north. Soldiers used to stand guard on watchtowers day and night, watching for any signs of danger. Today, the Great Wall is a famous tourist attraction. People from all over the world come to walk on its ancient stones. Standing on the wall, you can imagine how difficult it must have been to build such an enormous structure without modern machines. The Great Wall shows us what human determination and teamwork can achieve.",

            @"My friend Alice loves science experiments. Last week, she showed me how to make a volcano using baking soda and vinegar. First, we built a mountain shape out of clay and put an empty bottle inside. Then we filled the bottle halfway with warm water and added red food coloring to make it look like lava. When Alice poured in the baking soda, nothing happened yet. But as soon as she added the vinegar, foam started bubbling up and flowing down the sides of our clay mountain. It looked just like a real volcanic eruption! Alice explained that the baking soda and vinegar react together to produce carbon dioxide gas, which creates all the bubbles. Science is fascinating because it helps us understand why things happen in the world around us."
        },
        [Difficulty.Hard] = new()
        {
            // Hard English: ~400-800 words, complex sentences, grades 5-6 level
            @"The ocean covers more than seventy percent of Earth's surface, yet humans have explored less than five percent of it. The deep ocean remains one of the last great mysteries on our planet. In the darkest depths where sunlight never reaches, creatures have evolved in extraordinary ways. Some fish produce their own light through bioluminescence, glowing in the pitch-black water to attract prey or find mates. Others have enormous eyes that can detect the faintest glimmer far away. The pressure at the bottom of the ocean is hundreds of times greater than at the surface, yet strange organisms thrive there. Scientists continue to discover new species on every deep-sea expedition. Recently, researchers found bacteria that survive on chemicals released from underwater volcanoes instead of sunlight. These discoveries challenge our understanding of where life can exist and have even influenced the search for life on other planets. Protecting our oceans is crucial because they regulate Earth's climate, provide food for billions of people, and hold secrets we have only begun to uncover.",

            @"When Alexander Graham Bell invented the telephone in eighteen seventy-six, he probably could not imagine how his creation would transform human communication. Before telephones, sending a message across long distances took days or weeks by letter. Today, we can instantly talk to someone on the other side of the world through smartphones that fit in our pockets. The evolution of communication technology has been remarkable. From the first telegraph messages sent through wires to today's wireless internet connecting billions of devices, each innovation has brought people closer together. Social media allows us to share moments of our lives with friends anywhere on Earth. Video calls let us see loved ones' faces even when we cannot be physically present. However, this constant connectivity raises important questions about privacy, mental health, and the quality of our relationships. While technology gives us powerful tools to connect, it is up to us to use them wisely and maintain genuine human bonds that no device can replace.",

            @"Ancient Egypt was one of the greatest civilizations in human history. Along the banks of the Nile River, the Egyptians built magnificent pyramids that still stand after thousands of years. The Great Pyramid of Giza was the tallest building in the world for over three thousand eight hundred years. Egyptians developed a writing system called hieroglyphics, which used pictures to represent words and ideas. They also created papyrus, an early form of paper made from reeds that grew along the Nile. Egyptian society was highly organized, with a pharaoh ruling as both king and god. Skilled architects designed temples, doctors performed surgeries, and astronomers mapped the stars. Perhaps most impressive was their practice of mummification, preserving bodies for the afterlife with remarkable knowledge of chemistry and anatomy. Studying ancient Egypt teaches us about human ingenuity and reminds us that great achievements are possible through dedication, cooperation, and respect for knowledge passed down through generations.",

            @"Climate change is the defining challenge of our generation. Average temperatures around the world have risen by about one degree Celsius since the industrial revolution, and scientists warn that the pace is accelerating. Glaciers are melting, sea levels are rising, and extreme weather events like hurricanes, droughts, and floods are becoming more frequent and severe. The primary cause is the burning of fossil fuels like coal, oil, and gas, which releases carbon dioxide into the atmosphere. This greenhouse gas traps heat that would otherwise escape into space. The consequences affect every living thing on Earth. Polar bears lose their hunting grounds as ice melts. Coral reefs die when ocean waters become too warm and acidic. Farmers struggle with unpredictable weather patterns that destroy crops. Solving climate change requires action at every level. Governments must invest in renewable energy like solar and wind power. Industries need to reduce emissions and develop cleaner technologies. Each of us can contribute by conserving energy, reducing waste, and making environmentally conscious choices. The decisions we make today will determine what kind of planet we leave for our children and grandchildren."
        }
    };

    private static readonly Dictionary<Difficulty, List<string>> ChineseTexts = new()
    {
        [Difficulty.Easy] = new()
        {
            "春天来了，小草从泥土里探出嫩绿的小脑袋。花儿也争先恐后地开放了，有红的、黄的、紫的，五颜六色，美丽极了。蝴蝶在花丛中飞来飞去，好像在跳舞一样。",
            "我的书包里装着语文书、数学书和文具盒。每天早上，我背着书包高高兴兴地去上学。学校里有亲爱的老师和可爱的同学，大家一起学习，一起做游戏，真快乐！",
            "奶奶养了一只小花猫。它全身雪白，只有耳朵尖是黄色的。它的眼睛像两颗玻璃球，到了晚上还会发光呢！小花猫最喜欢吃鱼，每次看到鱼就喵喵叫个不停。",
            "今天天气真好，蓝蓝的天上飘着几朵白云。我和爸爸妈妈一起去公园放风筝。风筝飞得高高的，好像一只大鸟在空中自由自在地飞翔。我跑啊跑啊，开心极了！"
        },
        [Difficulty.Normal] = new()
        {
            "秋天是一个丰收的季节。田野里，金灿灿的稻谷笑弯了腰；果园里，红彤彤的苹果挂满了枝头。农民伯伯看着满仓的粮食，脸上露出了幸福的笑容。秋天的风轻轻吹过，树叶像一只只黄蝴蝶飘落下来，铺成了一条金色的小路。小朋友们踩在上面，发出沙沙的声音，好听极了。",
            "我们的学校有一个很大的图书馆。图书馆里有成千上万本书，有故事书、科学书、历史书，还有许多漂亮的画册。每当放学后，我就喜欢去那里看书。坐在安静的角落里，翻开一本心爱的书，仿佛进入了一个神奇的世界。书是人类进步的阶梯，读书让我学到了很多知识，也让我懂得了许多道理。",
            "妈妈每天都很辛苦。早上她要早早起床给我做早饭，送我上学后还要去上班。晚上回来，她要做饭、洗衣服、辅导我做作业。有时候我看妈妈累了，就会给她倒一杯水，帮她捶捶背。妈妈总是笑着说：你真是个懂事的好孩子。我想快快长大，以后好好照顾妈妈。",
            "端午节是中国传统节日之一。这一天，家家户户都要吃粽子、赛龙舟。粽子是用糯米做的，里面包着红枣或者肉，吃起来又香又糯。关于端午节的来历，还有一个感人的故事呢！相传古代有一位叫屈原的大诗人，他非常爱国，后来因为国家被敌人占领，悲愤地投江自尽了。人们为了纪念他，就在每年的五月初五划船把粽子扔进江里，希望鱼儿不要伤害他的身体。"
        },
        [Difficulty.Hard] = new()
        {
            "朱自清先生的《匆匆》是一篇优美的散文。燕子去了，有再来的时候；杨柳枯了，有再青的时候；桃花谢了，有再开的时候。但是，聪明的你告诉我，我们的日子为什么一去不复返呢？是有人偷了他们吧，那是谁？又藏在何处呢？是他们自己逃走了吧，现在又在哪里呢？我不知道他们给了我多少日子，但我的手确乎是渐渐空虚了。在默默里算着，八千多日子已经从我手中溜去，像针尖上一滴水滴在大海里，我的日子滴在时间的流里，没有声音，也没有影子。我不禁头涔涔而泪潸潸了。",
            "《从百草园到三味书屋》是鲁迅先生回忆童年生活的名篇。百草园是他儿时的乐园，那里有碧绿的菜畦，光滑的石井栏，高大的皂荚树，紫红的桑葚。鸣蝉在树叶里长吟，肥胖的黄蜂伏在菜花上。如果移动到墙根附近，还可以按住斑蝥的脊梁，让它从后窍喷出一阵烟雾。何首乌藤和木莲藤缠绕着，何首乌有臃肿的根，有人说吃了可以成仙。冬天可以在雪地上捕鸟，扫开一块雪，露出地面，用一支短棒支起一面大的竹筛来，下面撒些秕谷，棒上系一条长绳，人远远地牵着，看鸟雀下来啄食，走到竹筛底下的时候，将绳子一拉，便罩住了。",
            "老舍先生的《济南的冬天》写得真好。济南的冬天是没有风声的。对于一个刚由伦敦回来的人，像我，冬天要能看得见日光，便觉得是怪事；济南的冬天是响晴的。自然，在热带的地方，日光永远是那么毒，响亮的天气反有点叫人害怕。可是，在北中国的冬天，而能有温晴的天气，济南真算个宝地。设若单单是有阳光，那也算不了出奇。请闭上眼睛想：一个老城，有山有水，全在天底下晒着阳光，暖和安适地睡着，只等春风来把它们唤醒，这是不是个理想的境界？"
        }
    };

    private static readonly List<string> SpeedTexts = new()
    {
        "The early bird catches the worm, but the second mouse gets the cheese. Success comes to those who prepare well and act quickly, but sometimes luck plays a role too.",
        "Typing speed is measured in words per minute, where five characters equal one word on average. Professional typists can reach speeds of eighty to one hundred words per minute with high accuracy.",
        "Stay focused, keep your rhythm, and let your fingers find the keys without conscious thought. Practice makes perfect, and consistent daily effort builds muscle memory that lasts a lifetime.",
        "敏捷的手指来自于成千上万次重复练习，让正确的按键动作成为身体的本能反应。每一次准确的敲击都在为更快的速度打下坚实的基础。",
        "Good typing posture matters: keep your feet flat on the floor, back straight, wrists slightly elevated, and eyes on the screen rather than the keyboard. These habits prevent strain and boost long-term performance."
    };

    /// <summary>
    /// 小学生打字题库：分类 + 标题 + 正文 + 难度。
    /// 中文文章使用小学1-6年级语文课文风格，每篇100-1000字；
    /// 英文文章使用小学3-6年级英语阅读风格，每篇100-1000词。
    /// 下拉框会按"分类 · 标题"列出每一篇供用户挑选。
    /// </summary>
    public sealed record BuiltInArticle(string Category, string Title, string Body, Difficulty Difficulty);

    private static readonly List<BuiltInArticle> BuiltInArticlesList = new()
    {
        // ==================== 中文题库（小学1-6年级课文风格）====================

        // —— 一年级（简单短文，100-200字）——
        new("一年级·四季", "春天来了", "春天来了，春天来了。小草从地下探出头来，那是春天的眉毛吧？早开的野花一朵两朵，那是春天的眼睛吧？树木吐出点点嫩芽，那是春天的音符吧？解冻的小溪丁丁冬冬，那是春天的琴声吧？春天来了，我们看到了她，我们听到了她，我们闻到了她，我们触到了她在柳枝上荡秋千，在风筝尾巴上摇啊摇，她在喜鹊、杜鹃嘴里叫，在桃花、杏枝头笑……", Difficulty.Easy),
        new("一年级·四季", "夏天的雨", "夏天的雨哗啦啦，池塘里的荷花开了。粉红色的花瓣像小姑娘的脸蛋，碧绿的荷叶像一把把小伞。青蛙蹲在荷叶上呱呱地唱歌，小鱼在水里快活地游来游去。蜻蜓飞过来停在荷叶尖上，多美的一幅画呀！", Difficulty.Easy),
        new("一年级·四季", "秋天的落叶", "秋天到了，天气凉了。一片片黄叶从树上落下来，有的像蝴蝶在空中跳舞，有的像小船在地上漂。小朋友们在操场上捡落叶，拼成了各种图案：有金鱼，有小兔，还有太阳。大家笑着喊着，玩得可高兴了！", Difficulty.Easy),
        new("一年级·四季", "冬天的雪", "下雪啦，下雪啦！雪地里来了一群小画家。小鸡画竹叶，小狗画梅花，小鸭画枫叶，小马画月牙。不用颜料不用笔，几步就成一幅画。青蛙为什么没参加？他在洞里睡着啦！", Difficulty.Easy),

        new("一年级·动物", "可爱的小白兔", "我家养了一只小白兔。它浑身雪白雪白的，像穿了一件白皮袄。两只长长的耳朵竖着，一双眼睛红红的，像两颗红宝石。一张三瓣嘴总是一动一动的，好像在说什么。它最爱吃胡萝卜和青菜，吃得津津有味的样子可爱极了！", Difficulty.Easy),
        new("一年级·动物", "小猫钓鱼", "猫妈妈带着小猫去河边钓鱼。一只蜻蜓飞来了，小猫放下鱼竿去捉蜻蜓。蜻蜓飞走了，小猫空着手回来了。一只蝴蝶飞来了，小猫又放下鱼竿去捉蝴蝶。蝴蝶飞走了，小猫还是空着手回来了。猫妈妈说：钓鱼要一心一意。小猫听了猫妈妈的话，一心一意地钓起鱼来。不一会儿，小猫就钓到了一条大鱼！", Difficulty.Easy),
        new("一年级·动物", "数鸭子", "门前大桥下，游过一群鸭。快来快来数一数，二四六七八。嘎嘎嘎嘎，真呀真多呀，数不清到底多少鸭。赶鸭老爷爷，胡子白花花，唱呀唱着家乡戏，还会说笑话。小孩小孩快快上学去，别考个鸭蛋抱回家！", Difficulty.Easy),
        new("一年级·动物", "小蝌蚪找妈妈", "池塘里有一群小蝌蚪，大大的脑袋，黑灰色的身子，甩着长长的尾巴，快活地游来游去。小蝌蚪看见鲤鱼阿姨，连忙迎上去问：我们的妈妈在哪里？鲤鱼阿姨说：你们的妈妈四条腿，宽嘴巴。小蝌蚪游啊游，看见了乌龟，连忙叫：妈妈！乌龟笑着说：我不是你们的妈妈，你们的妈妈头顶上有两只大眼睛，披着绿衣裳。最后，小蝌蚪终于找到了妈妈——原来是一只大青蛙！", Difficulty.Easy),

        new("一年级·校园", "上学歌", "太阳当空照，花儿对我笑。小鸟说早早早，你为什么背上小书包？我去上学校，天天不迟到。爱学习爱劳动，长大要为人民立功劳！", Difficulty.Easy),
        new("一年级·校园", "我的老师", "我的老师姓王，她很漂亮。她的头发长长的，眼睛大大的，说话声音很好听。上课时，她教我们认字、算术。下课时，她和我们一起做游戏。我有不会的题目，老师总是耐心地教我。我爱我的老师！", Difficulty.Easy),
        new("一年级·校园", "升国旗", "星期一的早晨，全校同学排着整齐的队伍来到操场上。国旗班的同学迈着整齐的步伐走上升旗台。国歌奏响了，五星红旗冉冉升起。全体师生行注目礼，少先队员敬队礼。看着鲜艳的五星红旗，我心里感到无比自豪！", Difficulty.Easy),
        new("一年级·校园", "课间十分钟", "下课铃响了，同学们像小鸟一样飞出教室。操场上一下子热闹起来。有的跳绳，有的踢毽子，有的拍皮球，还有的捉迷藏。大家玩得满头大汗，脸上却挂着开心的笑容。上课铃响了，同学们依依不舍地回到教室，准备开始下一节课的学习。", Difficulty.Easy),

        new("一年级·家庭", "我的家", "我有一个温暖的家。爸爸工作很努力，妈妈做的饭菜最好吃。爷爷奶奶住在乡下，他们种了很多蔬菜水果。每到周末，我们就去看望他们。一家人围坐在一起吃饭聊天，真幸福呀！", Difficulty.Easy),
        new("一年级·家庭", "帮妈妈做事", "今天是妈妈的生日，我想给妈妈一个惊喜。早上我悄悄起床，叠好了被子，还把自己的房间收拾干净。妈妈醒来看到整洁的房间，惊讶地睁大了眼睛。我跑过去抱住妈妈说：妈妈，生日快乐！妈妈感动得眼圈都红了。", Difficulty.Easy),
        new("一年级·家庭", "爷爷的故事", "每天晚上睡觉前，爷爷都会给我讲故事。爷爷讲过孙悟空大闹天宫，讲过司马光砸缸救人，还讲过孔融让梨的故事。我最喜欢听爷爷讲故事了，那些故事让我明白了很多道理。我要做一个懂事的好孩子！", Difficulty.Easy),

        new("一年级·节日", "过春节", "春节到了，到处张灯结彩。家家户户贴上了红红的春联，挂上了大大的红灯笼。除夕夜，全家人围坐在一起吃团圆饭，有饺子、有鱼、有鸡，还有好多好吃的菜。吃完饭，大人给我们发压岁钱，我们穿着新衣服放鞭炮，真热闹啊！", Difficulty.Easy),
        new("一年级·节日", "中秋节", "八月十五是中秋节。晚上的月亮又圆又亮，像一个大盘子挂在天上。我们一家人坐在院子里赏月。桌上摆着月饼和各种水果。奶奶一边吃月饼一边讲嫦娥奔月的故事。月亮里的嫦娥一定很孤单吧，我真想去陪陪她。", Difficulty.Easy),
        new("一年级·节日", "儿童节", "六月一日是我们自己的节日。学校里举办了精彩的表演。有唱歌的，有跳舞的，还有演小品的。我在台上朗诵了一首诗，台下掌声雷动。老师还给我们每人发了一份礼物。这个儿童节过得太开心了！", Difficulty.Easy),

        // —— 二年级（中等长度，200-350字）——
        new("二年级·自然", "美丽的西湖", "杭州的西湖闻名天下。湖水碧绿碧绿的，像一块巨大的翡翠。湖边种满了垂柳，柔软的枝条随风摆动，就像姑娘的长发在风中飘扬。湖中有三座小岛，远远望去就像三颗绿色的宝石镶嵌在湖面上。晴天，阳光洒在湖面上波光粼粼；雨天，山峦朦胧，别有一番韵味。难怪古人说：欲把西湖比西子，淡妆浓抹总相宜。西湖的美，一年四季都看不厌。春天桃花盛开，夏天荷花飘香，秋天桂花芬芳，冬天断桥残雪。无论什么时候来西湖，都能看到不同的美景。", Difficulty.Easy),
        new("二年级·自然", "雷雨", "满天的乌云黑沉沉地压下来。树上的叶子一动也不动，蝉一声也不出。忽然一阵大风，吹得树枝乱摆。一只蜘蛛从网上垂下来逃走了。闪电越来越亮，雷声越来越响。哗，哗，哗，雨下起来了。雨越下越大。往窗外望去，树哇，房子啊，都看不清了。渐渐地，渐渐地，雷声小了，雨声也小了。天亮起来了。打开窗户，清新的空气迎面扑来。雨停了。太阳出来了。一条彩虹挂在天空。蝉又叫了。蜘蛛又坐在网上。池塘里水满了，青蛙也叫起来了。", Difficulty.Easy),
        new("二年级·自然", "植物妈妈有办法", "孩子如果已经长大，就得告别妈妈四海为家。牛马有脚，鸟有翅膀，植物旅行靠什么办法？蒲公英妈妈准备了降落伞，把它送给自己的娃娃。只要有风轻轻吹过，孩子们就乘着风纷纷出发。苍耳妈妈给孩子穿上带刺的铠甲，只要挂住动物的皮毛，就能走到田野山洼。豌豆妈妈更有办法，她让豆荚晒在太阳底下，啪的一声，豆荚炸开，孩子们就蹦跳着离开妈妈。植物妈妈的办法很多很多，不信你就仔细观察。那里有许许多多的知识，粗心的小朋友却得不到它。", Difficulty.Easy),
        new("二年级·自然", "日月潭", "日月潭是我国台湾省最大的一个湖。它在台中附近的高山上。那里群山环绕，树木茂盛，周围有许多名胜古迹。日月潭很深，湖水碧绿。湖中央有个美丽的小岛，把湖分成两半，北边像圆圆的太阳，叫日潭；南边像弯弯的月亮，叫月潭。清晨，湖面上飘着薄薄的雾。天边的晨星和山上的点点灯光，隐隐约约地倒映在湖水中。中午，太阳高照，整个日月潭的美景和周围的建筑都清晰地展现出来。要是下起蒙蒙细雨，日月潭好像披上轻纱，周围的景物一片朦胧，就像童话中的仙境。", Difficulty.Normal),

        new("二年级·动物", "大象的耳朵", "大象有一对大耳朵，像扇子似的耷拉着。这一天，大象正在路上慢慢地走着，遇见了小兔子。小兔子说：大象大哥，您的耳朵耷拉着，准是病了！小猴子也说：是的，是的，您的耳朵真是毛病不小！大象也不信，也不理，继续往前走。遇见了小鹿。小鹿也说：大象啊，您的耳朵这么大，准是生病了吧！大象心想：大家都这么说，也许我真的病了。于是他用一根竹子把耳朵撑起来。结果，虫子们在大象耳边嗡嗡地飞，吵得大象头痛极了。唉，原来大象的耳朵耷拉着是有用的，可以驱赶虫子呢！", Difficulty.Normal),
        new("二年级·动物", "蜘蛛开店", "有一只蜘蛛，每天都蹲在网上等着小飞虫落在上面，好寂寞，好无聊啊。蜘蛛决定开一家商店。卖什么呢？就卖口罩吧，因为口罩织起来很简单。于是蜘蛛在一间小木屋外面挂了一个招牌，上面写着：口罩编织店，每位顾客一元钱。顾客来了，是一头河马！蜘蛛织啊织，足足忙了一个星期，才织完那个像房子一样大的口罩。蜘蛛累得趴倒在地上，心里想：还是卖袜子吧，袜子编织起来很简单。第二天，蜘蛛的招牌换了，上面写着：袜子编织店，每位顾客一元钱。可是顾客是一只四十二只脚的蜈蚣！蜘蛛吓得匆忙跑回网上。原来是那位长腿顾客来了！", Difficulty.Normal),
        new("二年级·动物", "青蛙卖泥塘", "青蛙住在烂泥塘里。他觉得这儿不怎么样，想把泥塘卖掉，换几个钱搬到城里去。于是他在泥塘边竖起一块牌子，上面写着：卖泥塘喽！卖泥塘喽！一只老牛走过来，看了看泥塘说：这泥塘好吗？就是周围的草太少啦。老牛不想买泥塘，走了。青蛙想了想，就在泥塘周围种上了草。过了些日子，泥塘周围长满了青草。一只野鸭飞来了，看了看泥塘说：这泥塘好吗？就是水太少啦。野鸭不想买泥塘，走了。青蛙就用竹子引来了泉水。后来，泥塘变得有草有水，还有鲜花和树木，青蛙再也不想卖泥塘了。", Difficulty.Normal),

        new("二年级·名人", "曹冲称象", "曹操得到一头大象。这头象又高又大，身子像一堵墙，腿像四根柱子。官员们一边看一边议论：这么大的象，到底有多重呢？有人说：造一杆大秤来称。可是谁提得起这杆大秤呢？又有人说：把大象宰了，割成一块一块再称。曹操听了直摇头。这时，七岁的曹冲站出来说：我有个办法。把大象赶到一艘大船上，看船身下沉多少，就沿着水面在船舷上画一条线。再把大象赶上岸，往船上装石头，等船下沉到画线的地方，称一下石头的重量，就知道大象有多重了。曹操微笑着点点头。果然称出了大象的重量。", Difficulty.Normal),
        new("二年级·名人", "司马光砸缸", "古时候有个孩子叫司马光。有一天，他和几个小朋友在院子里玩耍。院子里有一口大水缸，缸里装满了水。一个小朋友爬到缸沿上玩，一不小心掉进了大水缸里。别的小朋友都吓坏了，有的哭，有的喊，还有的跑去找大人。司马光没有慌，他搬起一块大石头，使劲向水缸砸去。砰的一声，水缸破了，水流了出来，掉进缸里的小朋友得救了。大家都夸司马光聪明勇敢，遇事不慌张。这个故事告诉我们，遇到危险时要冷静思考，想办法解决问题。", Difficulty.Normal),

        new("二年级·生活", "我是什么", "我会变。太阳一晒，我就变成汽，升到空中，我又变成无数极小极小的点儿，连成一片，在空中漂浮，人们管我叫云。有时候我穿白衣服，有时候我穿黑衣服，早晨和傍晚我又把红袍披在身上。人们管我叫霞。我在空中越升越高，体温越来越低，变成了无数小水滴，聚在一起变成雨。有时候我变成小硬球打下来，人们管我叫雹子。到了冬天，我变成小花朵飘下来，人们又管我叫雪。你们猜，我是什么？对啦，我就是水！", Difficulty.Easy),
        new("二年级·生活", "玲玲的画", "玲玲明天要去参加美术比赛，她画了一只贪吃的小熊猫，正在啃竹叶，可爱极了。玲玲满意地端详着自己的画，不小心水彩笔啪的一声掉在了纸上，把画弄脏了。玲玲伤心地哭了起来。爸爸拿起画仔细看了看说：别哭，你看这里正好可以画一只小花狗，它在追蝴蝶呢！玲玲听了眼睛一亮，赶紧拿起画笔添了几笔。哇，画比以前更好看了！玲玲明白了，很多事情看起来是坏事，但只要动脑筋，也能变成好事。", Difficulty.Normal),

        // —— 三年级（较长文章，300-500字）——
        new("三年级·名家名篇", "荷花（叶圣陶）", "清晨，我到公园去玩，一进门就闻到一阵清香。我赶紧往荷花池边跑去。荷花已经开了不少了。荷叶挨挨挤挤的，像一个个碧绿的大圆盘。白荷花在这些大圆盘之间冒出来。有的才展开两三片花瓣儿。有的花瓣儿全展开了，露出嫩黄色的小莲蓬。有的还是花骨朵儿，看起来饱胀得马上要破裂似的。这么多的白荷花，一朵有一朵的姿势。看看这一朵，很美；看看那一朵，也很美。如果把眼前的一池荷花看作一大幅活的画，那画家的本领可真了不起。我忽然觉得自己仿佛就是一朵荷花，穿着雪白的衣裳，站在阳光里。一阵微风吹来，我就翩翩起舞，雪白的衣裳随风飘动。不光是我一朵，一池的荷花都在舞蹈。风过了，我停止了舞蹈，静静地站在那儿。蜻蜓飞过来，告诉我清早飞行的快乐。小鱼在脚下游过，告诉我昨夜做的好梦……过了一会儿，我才记起我不是荷花，我是在看荷花呢。", Difficulty.Normal),
        new("三年级·名家名篇", "赵州桥", "河北省赵县的洨河上，有一座世界闻名的石拱桥，叫安济桥，又叫赵州桥。它是隋朝的石匠李春设计和参加建造的，到现在已经有一千四百多年了。赵州桥非常雄伟。桥长五十多米，有九米多宽，中间行车马，两旁走人。这么长的桥，全部用石头砌成，下面没有桥墩，只有一个拱形的大桥洞，横跨在三十七米多宽的河面上。大桥洞顶上的左右两边，还各有两个拱形的小桥洞。平时，河水从大桥洞流过，发大水的时候，河水还可以从四个小桥洞流过。这种设计，在建桥史上是一个创举，既减轻了流水对桥身的冲击力，使桥不容易被大水冲坏，又减轻了桥身的重量，节省了石料。这座桥不但坚固，而且美观。桥面两侧有石栏，栏板上雕刻着精美的图案：有的刻着两条相互缠绕的龙，嘴里吐出美丽的水花；有的刻着两条飞龙，前爪相互抵着，各自回首遥望；还有的刻着双龙戏珠。所有的龙似乎都在游动，真像活了一样。赵州桥表现了劳动人民的智慧和才干，是我国宝贵的历史遗产。", Difficulty.Normal),
        new("三年级·名家名篇", "富饶的西沙群岛", "西沙群岛是南海上的一群岛屿，是我国的海防前哨。那里风景优美，物产丰富，是个可爱的地方。西沙群岛一带海水五光十色，瑰丽无比：有深蓝的，淡青的，浅绿的，杏黄的。一块块，一条条，相互交错着。因为海底高低不平，有山崖，有峡谷，海水有深有浅，从海面看色彩就不同了。海底的岩石上长着各种各样的珊瑚，有的像绽开的花朵，有的像分枝的鹿角。海参到处都是，在海底懒洋洋地蠕动。大龙虾全身披甲，划过来，划过去，样子挺威武。鱼成群结队地在珊瑚丛中穿来穿去，好看极了。有的全身布满彩色的条纹；有的头上长着一簇红缨；有的周身像插着好些扇子，游动的时候飘飘摇摇；有的眼睛圆溜溜的，突然往旁边一闪，就不见了；有的身上长满了刺，鼓起气来像皮球一样圆。各种各样的鱼多得数不清。正像人们说的那样，西沙群岛的一半是水，一半是鱼。", Difficulty.Normal),

        new("三年级·科普", "蜜蜂（法布尔）", "听说蜜蜂有辨认方向的能力，无论飞到哪里，它总是可以回到原处。我想做个实验。一天，我在我家草料棚的蜂窝里捉了一些蜜蜂，把它们放在纸袋里。为了证实飞回巢的蜜蜂是我放飞的，我在它们的背上做了白色的记号。然后，我叫小女儿在蜂窝旁等着，自己带着做了记号的二十只蜜蜂，走了四公里路，打开纸袋，把它们放出来。那些被闷了好久的蜜蜂向四面飞散，好像在寻找回家的方向。这时候刮起了狂风，蜜蜂飞得很低，几乎要触到地面，大概这样可以减少阻力。我想，它们飞得这么低，怎么能看到遥远的家呢？在回家的路上，我推测蜜蜂可能找不到家了。没等我跨进家门，小女儿就冲过来，脸红红的，看上去很激动。她高声喊道：有两只蜜蜂飞回来了！它们两点四十分回到蜂窝里，肚皮上还沾着花粉呢。这样，二十只蜜蜂中，十七只没有迷失方向，准确无误地回到了家。尽管它们逆风而飞，沿途都是一些陌生的景物，但它们确确实实飞回来了。蜜蜂靠的不是超常的记忆力，而是一种我无法解释的本能。", Difficulty.Normal),
        new("三年级·科普", "花钟", "鲜花朵朵，争奇斗艳，芬芳迷人。要是我们留心观察就会发现，一天之内，不同的花开放的时间是不同的。凌晨四点，牵牛花吹起了紫色的小喇叭；五点左右，艳丽的蔷薇绽开了笑脸；七点，睡莲从梦中醒来；中午十二点左右，午时花开花了；下午三点，万寿菊欣然怒放；傍晚六点，烟草花在暮色中苏醒；月光花在七点左右舒展开自己的花瓣；夜来香在晚上八点开花；昙花却在九点左右含笑一现。不同的植物为什么开花的时间不同呢？原来，植物开花的时间与温度、湿度、光照有着密切的关系。还有的花需要昆虫传播花粉，所以开花时间与昆虫活动的时间相吻合。一位植物学家曾有意把不同时间开放的花种在一起，把花圃修建得像钟面一样，组成花的时钟。这些花在二十四小时内陆续开放。你只要看看什么花刚刚开放，就知道大致是几点钟了。", Difficulty.Normal),
        new("三年级·科普", "海底世界", "你可知道，大海深处是怎样的吗？海面上波涛澎湃的时候，海底依然很宁静。最大的风浪也只能影响到海面以下几十米深。阳光射不到深海，那里一片黑暗。然而在这漆黑的深处，却有许多光点像闪烁的星星，那是有发光器官的深水鱼在游动。海底是否没有一点儿声音呢？不是的。海底的动物常常在窃窃私语，只是我们听不到而已。如果你用上特制的水中听音器，就能听到各种声音：有的像蜜蜂一样嗡嗡，有的像小鸟一样啾啾，有的像小狗一样汪汪，有的还在打鼓。它们吃东西的时候发出一种声音，行进的时候发出另一种声音，遇到危险还会发出警报。海里的动物大约有三万种，它们各有各的活动方法。海参靠肌肉伸缩爬行，每小时只能前进四米。有一种鱼身体像梭子，每小时能游几十公里，攻击其他动物的时候，速度比普通的火车还快。乌贼和章鱼能突然向前方喷水，利用水的反推力迅速后退。还有些贝类自己不动，却能巴在轮船底下做免费的长途旅行。", Difficulty.Normal),

        new("三年级·童话", "陶罐和铁罐", "国王的御厨里有两个罐子，一个是陶的，一个是铁的。骄傲的铁罐看不起陶罐，常常奚落它。你敢碰我吗，陶罐子！铁罐傲慢地问。不敢，铁罐兄弟。陶罐谦虚地回答。我就知道你不敢，懦弱的东西！铁罐说，带着更加轻蔑的神气。我确实不敢碰你，但并不是懦弱。陶罐争辩说，我们生来就是盛东西的，并不是来互相碰撞的。说到盛东西，我不见得比你差。再说……住嘴！铁罐愤怒地吼道，你怎么敢和我相提并论！你等着吧，要不了几天，你就会破成碎片，我却永远在这里，什么也不怕。何必这样说呢？陶罐温和地说，我们还是和睦相处吧，有什么可吵的呢！和你在一起我感到羞耻，你算什么东西！铁罐说着，更加轻蔑地对待陶罐。时间在流逝，世界上发生了许多事情。王朝覆灭了，宫殿倒塌了，两个罐子遗落在荒凉的场地上，上面覆盖了厚厚的尘土。许多年代过去了，人们发现了那个陶罐。哟，这里有一个罐子！一个人惊讶地说。真的，一只陶罐！其他的人都高兴地叫起来。捧起陶罐，倒掉里面的泥土，擦洗干净，它还是那样光洁，朴素，美观。多美的陶罐！一个人小心地把陶罐清理干净。谢谢你们！陶罐兴奋地说，我的兄弟铁罐就在我旁边，请你们把它掘出来吧，它一定也闷得够呛了。人们立即动手，翻来覆去，把土都掘遍了，但是连铁罐的影子也没见到。", Difficulty.Normal),
        new("三年级·童话", "狮子和鹿", "丛林中，住着一只漂亮的鹿。有一天，鹿口渴了，找到一个池塘，痛痛快快地喝起水来。池水清清的，像一面镜子。鹿忽然发现了自己倒映在水中的影子：咦，这是我吗？鹿摆摆身子，水中的倒影也跟着摆摆身子。他从来没想到自己是这么漂亮！他不着急离开了，对着池水欣赏自己的美丽：啊！我的身段多么匀称，我的角多么精美别致，好像两束美丽的珊瑚！清风吹过，池水泛起了层层波纹。鹿忽然看到了自己的腿，不禁撅起了嘴，皱起了眉头：唉，这四条腿太细了，怎么配得上这两只美丽的角呢！鹿开始抱怨起自己的腿来。就在他没精打采地准备离开时，远处传来一阵脚步声。他猛回头，哎呀，一头狮子正悄悄地向自己逼近！鹿不敢犹豫，撒开长腿就跑。有力的长腿在灌木丛中蹦来跳去，不一会儿就把凶猛的狮子远远地甩在了后面。就在狮子灰心丧气不想再追的时候，鹿的角却被树枝挂住了。狮子赶紧抓住这个机会猛扑上来。眼看就要追上了，鹿用尽全身力气一扯，才把两只角从树枝中挣脱出来，然后又拼命向前奔去。这次狮子再也没有追上。鹿跑到一条小溪边停下喘气，一边喝水一边自言自语地说：两只美丽的角差点儿送了我的命，可四条难看的腿却让我狮口逃生！", Difficulty.Normal),

        new("三年级·成长", "我不能失信", "一个星期日的早晨，宋庆龄一家吃过早餐，准备到父亲的一位朋友宋耀如家里去。小庆龄很高兴，因为伯伯家养的鸽子尖尖的嘴巴，红红的眼睛，漂亮极啦！伯伯还说准备送她一只呢！她刚走到门口，突然停住了脚步，想起一件事：今天上午要教小珍学叠花篮。父亲说：改天再教吧！明天再教也可以啊！不行！小庆龄坚决地说，我答应了别人就应该信守诺言。母亲说：那你就留下来吧！送你去伯伯家，见到他的鸽子再回来。不，妈妈。如果我去了伯伯家，小珍来了会扑空的。说完，她把手缩了回去。宋庆龄一个人在家里，一直等到下午，小珍才来。虽然没去看成可爱的鸽子，但宋庆龄心里很高兴，因为她做到了诚实守信。", Difficulty.Normal),
        new("三年级·成长", "一次成功的实验", "一位教育家要在小镇的小学做一个实验。他来到一所学校的校长室，请校长找三个学生。校长找到了六个学生，让他们排队等候。教育家拿出一个瓶子，又取出三个系着绳子的小铅锤。他把小铅锤一个一个地放进瓶子里，然后把瓶子放在地上。他对三个学生说：这个瓶子代表一口井，井底有毒气。现在你们三个人代表三个工人，必须在三分钟内把铅锤从瓶子里提出来，否则就会中毒身亡。准备好了吗？学生们齐声回答：准备好了！教育家拿起手表，说：开始！三个学生立刻行动起来。一个学生提起铅锤往上拉，另一个学生扶住瓶口不让瓶子倒下，第三个学生在一旁保护同伴的安全。他们配合默契，动作协调，不到一分钟就把三个铅锤全都提了出来。教育家问：你们为什么要这样做？学生回答说：如果我们只顾自己抢着往外拉铅锤，瓶子就会倒下，铅锤就会掉回井里，我们谁也活不了。教育家高兴地笑了：这个实验我做过很多次，每次都有人因为抢着逃生而被毒死。今天你们成功了，是因为你们懂得了团结合作的重要性。", Difficulty.Normal),

        // —— 四年级（更长文章，400-600字）——
        new("四年级·名家名篇", "观潮（赵宗成）", "钱塘江大潮自古以来被称为天下奇观。农历八月十八是一年一度的观潮日。这一天早上，我们来到了海宁市的盐官镇，据说这里是观潮最好的地方。我们随着观潮的人群登上了海塘大堤。宽阔的钱塘江横卧在眼前。江面很平静，越往东越宽，在雨后的阳光下笼罩着一层蒙蒙的薄雾。镇海古塔、中山亭和观潮台屹立在江边。远处几座小山在云雾中若隐若现。江潮还没有来，海塘大堤上早已人山人海。大家昂首东望，等着，盼着。午后一点左右，从远处传来隆隆的响声，好像闷雷滚动。顿时人声鼎沸。有人告诉我们，潮来了！我们踮着脚向东望去，江面还是风平浪静，看不出有什么变化。过了一会儿，响声越来越大，只见东边水天相接的地方出现了一条白线。人群又沸腾起来。那条白线很快向我们移来，逐渐拉长，变粗，横贯江面。再近些，只见白浪翻滚，形成一道两丈多高的白色城墙。浪潮越来越近，犹如千万匹白色战马齐头并进，浩浩荡荡地飞奔而来；那声音如同山崩地裂，好像大地都被震得颤动起来。霎时，潮头奔腾西去，可是余波还在漫天卷地般涌来，江面上依旧风号浪吼。过了好久，钱塘江才恢复了平静。看看堤下，江水已经涨了两丈来高了。", Difficulty.Normal),
        new("四年级·名家名篇", "爬山虎的脚（叶圣陶）", "学校操场北边墙上满是爬山虎。我家也有爬山虎，从小院的西墙爬上去，在房顶上占了一大片地方。爬山虎刚长出来的叶子是嫩红的，不几天叶子长大，就变成嫩绿的。爬山虎的嫩叶不大引人注意，引人注意的是长大了的叶子。那些叶子绿得那么新鲜，看着非常舒服。叶尖一顺儿朝下，在墙上铺得那么均匀，没有重叠起来的，也不留一点儿空隙。一阵风拂过，一墙的叶子就漾起波纹，好看得很。以前我只知道这种植物叫爬山虎，可不知道它怎么爬。今年我注意了，原来爬山虎是有脚的。爬山虎的脚长在茎上。茎上长叶柄的地方，反面伸出枝状的六七根细丝，每根细丝像蜗牛的触角。细丝跟新叶子一样，也是嫩红的。这就是爬山虎的脚。爬山虎的脚步触着墙的时候，六七根细丝的头上就变成小圆片巴住墙。细丝原先是直的，现在弯曲了，把爬山虎的嫩茎拉一把，使它紧贴在墙上。爬山虎就是这样一脚一脚地往上爬。如果你仔细看那些细小的脚，你会想起图画上蛟龙的爪子。爬山虎的脚要是没触着墙，不几天就萎了，后来连痕迹也没有了。触着墙的，细丝和小圆片逐渐变成灰色。不要瞧不起那些灰色的脚，那些脚巴在墙上相当牢固，你的手指要想拉下它来，都得费点劲。", Difficulty.Normal),
        new("四年级·名家名篇", "颐和园（佚名）", "北京的颐和园是个美丽的大公园。走进大门，绕过大殿，就来到有名的长廊。这条长廊有七百多米长，分成二百七十三间。每一间的横槛上都有五彩的画，画着人物、花草、风景，几千幅画没有哪两幅是相同的。长廊两旁栽满了花木，这一种花还没谢，那一种又开了。微风从左边的昆明湖上吹来，使人神清气爽。走完长廊，就来到了万寿山下。抬头一看，一座八角宝塔形的建筑耸立在半山腰上，黄色的琉璃瓦闪闪发光，那就是佛香阁。下面的一排排金碧辉煌的宫殿，就是排云殿。登上万寿山，前面是昆明湖，静得像一面镜子，绿得像一块碧玉。游船画舫在湖面慢慢地滑过，几乎不留一点儿痕迹。向东远眺，隐隐约约可以望见几座古老的城楼和城里的白塔。从万寿山下来，就是昆明湖。昆明湖围着长长的堤岸，堤上有好几座式样不同的石桥，两岸栽着数不清的垂柳。湖中心有个小岛，远远望去，岛上一片葱绿。十七孔桥把两岸连接起来。桥栏杆上有上百根石柱，柱子上雕刻着姿态不一的狮子。这座石桥，古人设计得多么巧妙啊！", Difficulty.Normal),

        new("四年级·科普", "蝙蝠和雷达", "清朗的夜空出现两个亮点，随著时间渐渐靠近，终于看清楚了一架夜航的飞机和一只在夜空中飞行的蝙蝠。在漆黑的夜里，飞机怎么能安全飞行呢？原来是人们从蝙蝠身上得到了启示。科学家经过反复研究，终于揭开了蝙蝠能在夜里飞行的秘密。它一边飞，一边从嘴里发出一种声音。这种声音叫做超声波，人的耳朵是听不见的，蝙蝠的耳朵却能听见。超声波像波浪一样向前推进，遇到障碍物就反射回来，传到蝙蝠的耳朵里，蝙蝠就立刻改变飞行的方向。科学家模仿蝙蝠探路的方法，给飞机装上了雷达。雷达通过天线发出无线电波，无线电波遇到障碍物就反射回来，显示在荧光屏上。驾驶员从雷达的荧光屏上，能够看清楚前方有没有障碍物，所以飞机在夜里飞行也十分安全。其实除了飞机之外，现代生活中还有很多发明都是从动物身上得到的启示呢！比如人们根据萤火虫发明了冷光灯，根据鱼鳔发明了潜水艇，根据苍蝇的眼睛发明了复眼相机。大自然真是人类最好的老师啊！", Difficulty.Normal),
        new("四年级·科普", "呼风唤雨的世纪", "20世纪是一个呼风唤雨的世纪。是谁来呼风唤雨呢？当然是人类。靠什么呼风唤雨呢？靠的是现代科学技术。在20世纪一百年的时间里，人类利用现代科学技术获得了许多奇迹般的、出乎意料的发现和发明。正是这些发现和发明，使人类的生活大大改变，其改变的程度超过了人类历史上百万年的总和。人类在上百万年的历史中，一直生活在一个依赖自然的农耕社会。那时没有电灯，没有电视，没有汽车，也没有飞机。人们只能在神话中用千里眼顺风耳和腾云驾雾的神仙来寄托自己的美好愿望。20世纪的成就真可以用忽如一夜春风来，千树万树梨花开来形容。20世纪，人类登上月球，潜入深海，洞察百亿光年外的天体，探索原子核世界的奥秘；20世纪，电视、程控电话、因特网以及民航飞机、高速火车、远洋船舶等，日益把人类居住的星球变成联系紧密的地球村。人类生活的舒适和方便，是连过去的王公贵族也不敢想的。科学在改变着人类的精神文化生活，也在改变着人类的物质生活。", Difficulty.Normal),

        new("四年级·童话", "巨人的花园（王尔德）", "从前一个小村子里有座漂亮的花园。那里春天鲜花盛开，夏天绿树成荫，秋天鲜果飘香，冬天白雪一片。村里的孩子都喜欢到那里玩。花园的主人是个巨人，他外出旅行已经七年了。这天他回来了，看见孩子们在花园里玩耍，很生气：谁允许你们到这儿来的！都滚出去！巨人在花园四周砌起围墙，还立了一块告示牌：禁止入内，违者重惩。从此，孩子们的乐园变成了他们的禁地。春天来了，村子里又开出美丽的鲜花，可巨人的花园却仍然是冬天。花园里终年狂风大作，雪花飞舞。巨人裹着毯子，还瑟瑟发抖。他想：今年春天为什么这么冷这么荒凉呀！一天早晨，巨人被喧闹声吵醒了。他抬头一看，围墙破了个大洞，许多孩子正从那个洞钻进来。花园里又出现了春天的景象。可是巨人发现，每个角落都有一个孩子，唯独桃树角落还是冬天。原来那里有个小男孩太小了，够不着树枝，站在那儿哭泣。巨人轻轻地走过去，把小男孩抱到树上。这一抱，奇迹出现了！小男孩伸手一碰，树马上开花了，鸟儿们也飞来歌唱。巨人终于明白了：没有孩子的地方就没有春天。他推倒了围墙，把花园给了孩子们。从那以后，巨人和孩子们一起在花园里生活，非常快乐。", Difficulty.Normal),
        new("四年级·童话", "小木偶的故事", "老木匠做了一个小木偶。小木偶有鼻子有眼，能走路会说话，可是老木匠忘了给他装上表情器官。不管遇到什么事，小木偶都是一副笑嘻嘻的表情。有一天，老木匠让小木偶背着书包去上学。小木偶走在街上，一只小红狐跑过来，假装摔倒在地。小木偶急忙去扶他，小红狐却趁机偷走了小木偶的新书包。小木偶还是笑嘻嘻的，不知道该怎么办。一只狗警察抓住了小红狐，把书包还给了小木偶。可是小木偶还是笑嘻嘻的，连句感谢的话都不会说。回到家，老木匠知道了这件事，叹了口气说：唉，光有一副笑面孔是不行的，还得学会喜怒哀乐才行啊！于是老木匠给小木偶装上了各种表情。从那以后，小木偶该笑的时候笑，该哭的时候哭，成了一个真正有感情的孩子。", Difficulty.Easy),

        new("四年级·爱国", "为中华之崛起而读书", "周恩来少年时代在沈阳东关模范学校读书。有一天，魏校长亲自为学生上修身课。题目是：你们为什么而读书？有人回答：为明礼而读书。有人回答：为做官而读书。还有人回答：为挣钱而读书。有人回答：为吃饭而读书。当问到周恩来的时候，他清晰而坚定地回答道：为中华之崛起而读书！魏校长没有想到，竟然会有如此出众的学生。他为有志于振兴中华的学生而感到欣慰。周恩来是的，中华民族在当时确实面临着严重的危机。外国列强侵略中国，国家贫穷落后，人民生活在水深火热之中。少年周恩来目睹了这一切，心中燃起了救国的火焰。他知道，只有努力学习，掌握本领，才能让中国强大起来。这句话不仅表达了周恩来的远大志向，也激励了无数中国青年为国家富强而奋斗。后来，周恩来真的做到了。他为中国人民的革命事业奋斗了一生，成为深受人民爱戴的好总理。", Difficulty.Normal),

        // —— 五年级（复杂文章，500-800字）——
        new("五年级·名家名篇", "落花生（许地山）", "我们家的后园有半亩空地。母亲说：让它荒着怪可惜的，你们那么爱吃花生，就开辟出来种花生吧。我们姐弟几个都很高兴，买种、翻地、播种、浇水，没过几个月，居然收获了。母亲说：今晚我们过一个收获节，请你们的父亲也来尝尝我们的新花生好不好？母亲把花生做成了好几样食品，还吩咐就在后园的茅亭里过这个节。那天晚上天色不大好，可是父亲也来了，实在很难得。父亲说：你们爱吃花生吗？我们争着答应：爱！谁能把花生的好处说出来？姐姐说：花生的味儿美。哥哥说：花生可以榨油。我说：花生的价钱便宜，谁都可以买来吃，都喜欢吃。这就是花生的好处。父亲说：花生的好处很多，有一样最可贵：它的果实埋在地里，不像桃子、石榴、苹果那样，把鲜红嫩绿的果实高高地挂在枝上，使人一见就生爱慕之心。你们看它矮矮地长在地上，等到成熟了，也不能立刻分辨出来它有没有果实，必须挖起来才知道。所以我们都要做有用的人，不要做只讲体面而对别人没有好处的人。我说：那么，人要做有用的人，不要做只讲体面的人。父亲说：对，这是我对你们的希望。我们谈到深夜才散。花生做的食品都吃完了，父亲的话却深深地印在我的心上。", Difficulty.Normal),
        new("五年级·名家名篇", "珍珠鸟（冯骥才）", "真好！朋友送我一对珍珠鸟。我把这对鸟儿放在一个简易的竹条编成的笼子里，笼内还有一卷干草，那是小鸟舒适又温暖的巢。我把它挂在窗前。那儿还有一大盆异常茂盛的法国吊兰。我便用吊兰长长的、串生着小绿叶的垂蔓蒙盖在鸟笼上，它们就像躲进深幽的丛林一样安全；从中传出的笛儿般又细又亮的叫声，也就格外轻松自在了。小鸟的影子就在这中间隐约闪动，看不完整，有时连笼子也看不出，却见它们可爱的鲜红小嘴儿从绿叶中伸出来。我很少扒开叶蔓瞧它们，它们便渐渐敢伸出小脑袋瞅瞅我。我们就这样一点点熟悉了。三个月后，那一团愈发繁茂的绿蔓里边，发出一种尖细又娇嫩的鸣叫。我猜到，是它们有了雏儿。我呢，决不掀开叶片往里看，连添食加水时也不睁大好奇的眼去惊动它们。过不多久，忽然有一个小脑袋从叶间探出来。更小哟，雏儿！正是这个小家伙！它小，很容易受惊。起先，这小家伙只在笼子四周活动，随后就在屋里飞来飞去，一会儿落在柜顶上，一会儿神气十足地站在书架上，啄着书背上那些大文豪的名字。后来，它完全放心了。索性用那涂了蜡似的小红嘴，嗒嗒地啄着我颤动的笔尖。我用手抚一抚它细腻的绒毛，它也不怕，反而友好地啄两下我的手指。有一次，它居然跳进我的空茶杯里，隔着透明光亮的杯子瞅着我。它不怕我了，完全信赖我。有一天，我伏案写作时，它居然落到我的肩上。我手中的笔不觉停了，生怕惊跑它。待一会儿，扭头看，这小家伙竟趴在我的肩头睡着了，银灰色的眼睑盖住了眸子，小红脚刚好被胸脯上长长的绒毛盖住。我轻轻抬一抬肩，它没醒，睡得好熟！还咂咂嘴，难道在做梦！我笔尖一动，流泻下一时的感受：信赖，往往创造出美好的境界。", Difficulty.Normal),
        new("五年级·名家名篇", "春（朱自清）", "盼望着，盼望着，东风来了，春天的脚步近了。一切都像刚睡醒的样子，欣欣然张开了眼。山朗润起来了，水涨起来了，太阳的脸红起来了。小草偷偷地从土里钻出来，嫩嫩的，绿绿的。园子里，田野里，瞧去，一大片一大片满是的。坐着，躺着，打两个滚，踢几脚球，赛几趟跑，捉几回迷藏。风轻悄悄的，草软绵绵的。桃树、杏树、梨树，你不让我，我不让你，都开满了花赶趟儿。红的像火，粉的像霞，白的像雪。花里带着甜味儿；闭了眼，树上仿佛已经满是桃儿、杏儿、梨儿。花下成千成百的蜜蜂嗡嗡地闹着，大小的蝴蝶飞来飞去。野花遍地是：杂样儿，有名字的，没名字的，散在草丛里，像眼睛，像星星，还眨呀眨的。吹面不寒杨柳风，不错的，像母亲的手抚摸着你。风里带来些新翻的泥土的气息，混着青草味儿，还有各种花的香，都在微微润湿的空气里酝酿。鸟儿将巢安在繁花嫩叶当中，高兴起来了，呼朋引伴地卖弄清脆的喉咙，唱出婉转的曲子，跟清风流水应和着。牛背上牧童的短笛，这时候也成天嘹亮地响着。雨是最寻常的，一下就是三两天。可别恼。看，像牛毛，像花针，像细丝，密密地斜织着，人家屋顶上全笼着一层薄烟。树叶儿却绿得发亮，小草儿也青得逼你的眼。傍晚时候，上灯了，一点点黄晕的光，烘托出一片安静而和平的夜。在乡下，小路上，石桥边，有撑起伞慢慢走着的人，地里还有工作的农民，披着蓑戴着笠。他们的房屋稀稀疏疏的，在雨里静默着。天上风筝渐渐多了，地上孩子也多了。城里乡下，家家户户，老老小小，也赶趟儿似的，一个个都出来了。舒活舒活筋骨，抖擞抖擞精神，各做各的一份事去。一年之计在于春，刚起头儿，有的是工夫，有的是希望。春天像刚落地的娃娃，从头到脚都是新的，它生长着。春天像小姑娘，花枝招展的，笑着，走着。春天像健壮的青年，有铁一般的胳膊和腰脚，领着我们上前去。", Difficulty.Hard),

        new("五年级·科普", "鲸", "不少人见过鲸，都说鲸是鱼。其实它不是鱼，而是哺乳动物。鲸生活在海洋里，因为体形像鱼，许多人管它叫鲸鱼。鲸非常大，最大的鲸有十六万多公斤重，最小的也有两千公斤。我国捕获过一头近四万公斤重的鲸，有十七米长，一条舌头就有十几头大肥猪那么重。它要是张开嘴，人站在它嘴里，举起手来还摸不到它的上腭，四个人围着桌子坐在它的嘴里看书，还显得很宽敞。鲸生活在海洋里，因为体形像鱼，不少人误认为它是鱼。其实它不属于鱼类，而是哺乳动物。鲸的种类很多，总的来说可以分为两大类：一类是须鲸，没有牙齿；一类是齿鲸，有锋利的牙齿。鲸的身子这么大，它吃什么？须鲸主要吃虾和小鱼。它们在海洋里游的时候，张着大嘴，把许多小鱼小虾连同海水一起吸进嘴里，然后闭上嘴，把海水从须板中间滤出来，把小鱼小虾吞进肚子里，一顿就可以吃两千多公斤。齿鲸主要吃大鱼和海兽。它们遇到大鱼和海兽就凶猛地扑上去，用锋利的牙齿咬住，很快就吃掉了。有一种号称海中之王的虎鲸，常常好几十头结成一群，共同捕食。鲸跟牛羊一样用肺呼吸，这也说明它不属于鱼类。鲸的鼻孔长在脑袋顶上，呼吸的时候浮出海面，从鼻孔喷出来的气形成一股水柱，就像花园里的喷泉一样。不同种类的鲸喷出的水柱形状不一样：须鲸的水柱是垂直的，又高又细；齿鲸的水柱是倾斜的，又粗又矮。有经验的人观察水柱的形状，就可以判断鲸的种类和大小。鲸是胎生的，幼鲸靠吃母鲸的奶长大，这些特征也说明鲸是哺乳动物。", Difficulty.Normal),
        new("五年级·科普", "新型玻璃", "随着社会的进步和科学技术的发展，玻璃的功能越来越多，用途也越来越广泛。在现代化的城市里，各种新型的玻璃发挥着重要的作用。夹丝玻璃非常坚硬，不易破碎，即使碎了也不会伤人。有些银行的大门和珠宝店的橱窗就用了这种玻璃。变色玻璃能够随着光线的变化而改变颜色。它能阻挡强烈的阳光，使人感到凉爽舒适。这种玻璃可以用在建筑物上，也可以用来制造眼镜。吸热玻璃能把大部分的热量吸收掉，使室内保持凉爽。在炎热的夏天，这种玻璃特别受欢迎。吃音玻璃能够消除噪音。如果在临街的建筑物的墙上装上这种玻璃，街上的噪音就不会影响室内的人们工作和休息了。还有一种叫做防弹玻璃的新型玻璃，它可以抵挡子弹的射击。在一些重要的场所，比如银行、博物馆，经常能看到这种玻璃。新型玻璃的出现改变了人们对玻璃的传统认识。玻璃不再只是用来制作门窗的材料，它已经成为现代科技的重要组成部分。相信在不久的将来，还会有更多功能奇特的新型玻璃问世，为我们的生活带来更多的便利。", Difficulty.Normal),

        new("五年级·爱国", "圆明园的毁灭", "圆明园在北京西北郊，是一座举世闻名的皇家园林。它由圆明园、万春园和长春园组成，所以也叫圆明三园。此外，还有许多小园分布在圆明园东、西、南三面，众星拱月般环绕在圆明园周围。圆明园中，有金碧辉煌的殿堂，有玲珑剔透的亭台楼阁；有象征着热闹街市的买卖街，也有象征着田园风光的山乡村野。园中许多景物都是仿照各地名胜建造的，如海宁的安澜园、苏州的狮子林、杭州西湖的平湖秋色；还有很多景物是根据古代诗人的诗情画意建造的，如蓬莱瑶台、武陵春色。园中不仅有民族建筑，还有西洋景观。漫步园内，有如漫游在天南海北，饱览着中外风景名胜；流连其间，仿佛置身在幻想的境界里。圆明园不但建筑宏伟，还收藏着最珍贵的历史文物。上自先秦时代的青铜礼器，下至唐、宋、元、明、清历代的名人书画和各种奇珍异宝。所以，它又是当时世界上最大的博物馆、艺术馆。一八六零年十月六日，英法联军侵入北京，闯进圆明园。他们把园内凡是能拿走的东西统统掠走，拿不动的就用大车或牲口搬运。实在运不走的就任意破坏、毁掉。为了销毁罪证，十月十八日和十九日，三千多名侵略者奉命在园内放火。大火连烧三天，烟云笼罩了整个北京城。我国这一园林艺术的瑰宝、建筑艺术的精华，就这样化成了一片灰烬。", Difficulty.Normal),

        // —— 六年级（深度文章，600-1000字）——
        new("六年级·名家名篇", "北京的春节（老舍）", "按照北京的老规矩，春节差不多在腊月的初旬就开始了。腊七腊八，冻死寒鸦，这是一年里最冷的时候。可是，到了严冬，不久便是春天了，所以人们并不因为寒冷而减少过年与迎春的热情。在腊八那天，家家都熬腊八粥。粥是用各种米、各种豆与各种干果熬成的。这不是粥，而是小型的农业展览会。除此之外，这一天还要泡腊八蒜。把蒜瓣放进醋里，封起来，为过年吃饺子用。到年底，蒜泡得色如翡翠，醋也色味双美，使人忍不住要多吃几个饺子。在北京，过年时，家家户户吃饺子。孩子们特别热心，因为他们可以参与包饺子的过程。二十三过小年，大人们忙着打扫屋子，送灶王爷上天。除夕真热闹。家家赶做年菜，到处酒肉香味。男女老少都穿起新衣，门外贴好红红的对联，屋里贴好各色的年画。除夕夜，家家灯火通宵，不许间断，鞭炮声日夜不绝。在外边做事的人，除非万不得已，必定赶回家来吃团圆饭。这一夜，除了很小的孩子，没有什么人睡觉，都要守岁。正月初一，男人们午前去拜年，女人们在家中接待客人。小贩们在广场上摆摊，卖各种玩具和食品。孩子们特别爱逛庙会，因为那里有各种表演和游戏。铺户在正月初六才开门，不过并不很忙。元宵节到了，处处悬灯结彩，整条大街像是办喜事，红火而美丽。有名的老铺子都要挂出几百盏灯来。一眨眼，到了残灯末庙，学生该去上学，大人又去照常做事。新年虽然在正月十九结束，但那种喜庆的气氛还久久留在人们心中。", Difficulty.Normal),
        new("六年级·名家名篇", "匆匆（朱自清）", "燕子去了，有再来的时候；杨柳枯了，有再青的时候；桃花谢了，有再开的时候。但是，聪明的你告诉我，我们的日子为什么一去不复返呢？是有人偷了他们罢，那是谁？又藏在何处呢？是他们自己逃走了罢，现在又到了哪里呢？我不知道他们给了我多少日子，但我的手确乎是渐渐空虚了。在默默里算着，八千多日子已经从我手中溜去，像针尖上一滴水滴在大海里，我的日子滴在时间的流里，没有声音，也没有影子。我不禁头涔涔而泪潸潸了。去的尽管去了，来的尽管来着，去来的中间又怎样地匆匆呢？早上我起来的时候，小屋里射进两三方斜阳。太阳他有脚啊，轻轻悄悄地挪移了，我也茫茫然跟着旋转。于是洗手的时候，日子从水盆里过去；吃饭的时候，日子从饭碗里过去；默默时，便从凝然的双眼前过去。我觉察他去的匆匆了，伸出手遮挽时，他又从遮挽的手边过去。天黑时，我躺在床上，他便伶伶俐俐地从我身上跨过，从我脚边飞去了。等我睁开眼和太阳再见，这算又溜走了一日。我掩着面叹息，但是新来的日子的影儿又开始在叹息里闪过了。在逃去如飞的日子里，在千门万户的世界里我能做什么呢？只有徘徊罢了，只有匆匆罢了。在八千多日的匆匆里，除徘徊外又剩些什么呢？过去的日子如轻烟，被微风吹散了，如薄雾，被初阳蒸融了。我留着些什么痕迹呢？我何曾留着像游丝样的痕迹呢？我赤裸裸来到这世界，转眼间也将赤裸裸地回去罢？但不能平的，为什么偏要白白走这一遭啊？你聪明的，告诉我，我们的日子为什么一去不复返呢？", Difficulty.Hard),
        new("六年级·名家名篇", "少年闰土（鲁迅）", "深蓝的天空中挂着一轮金黄的圆月，下面是海边的沙地，都种着一望无际的碧绿的西瓜。其间有一个十一二岁的少年，项带银圈，手捏一柄钢叉，向一匹猹尽力地刺去。那猹却将身一扭，反从他的胯下逃走了。这少年便是闰土。我认识他时也不过十多岁，离现在将三十年了。那时我的父亲还在世，家景也好，我正是一个少爷。那一年，我家是一件大祭祀的值年。这祭祀说是三十多年才能轮到一回，所以很郑重。正月里供祖像，供品很多，祭器很讲究，拜的人也很多，祭器也要请人去看，母亲给闰工管祭器。我早听到闰土这名字，而且知道他和我仿佛年纪，闰月生的，五行缺土，所以他的父亲叫他闰土。他是能装弶捉小鸟雀的。我于是日日盼望新年，新年到，闰土也就到了。好容易到了年末，有一日，母亲告诉我，闰土来了。我便飞跑地去看。他正在厨房里，紫色的圆脸，头戴一顶小毡帽，颈上套一个明晃晃的银项圈。这可见他的父亲十分爱他，怕他死去，所以在神佛面前许下愿心，用圈子将他套住了。他见人很怕羞，只是不怕我，没有旁人的时候，便和我说话，于是不到半日，我们便熟识了。我们那时候不知道谈些什么，只记得闰土很高兴，说是上城之后，见了许多没有见过的东西。第二日，我便要他捕鸟。他说：这不能。要大雪下了才行。我们沙地上下了雪，我扫出一块空地来，用短棒支起一个大竹匾，撒下秕谷，看鸟雀下来啄食，我走过去将绳子一拉，便罩住了。什么鸟都有：稻鸡，角鸡，鹁鸪，蓝背……闰土的心里有无穷无尽的希奇的事，都是我往常的朋友所不知道的。", Difficulty.Hard),

        new("六年级·科普", "宇宙生命之谜", "古时候，科学不发达，人们向往着天上的世界。于是有了嫦娥奔月、仙女下凡、蟠桃盛会等神话故事。这些故事说明了人们渴望到宇宙中去探索。当然，由于科学水平的限制，那时候的人们不可能实现这些梦想。随着科学的发展，人们的梦想逐步变成了现实。如今，人造卫星上天了，宇航员乘坐飞船到达月球又返回地球，探测器降落在火星和金星表面。这些都表明，人类正在不断地探索宇宙的奥秘。哪些天体上可能有生命存在呢？这个天体又必须具备什么样的条件呢？了解了生命起源的过程之后，这个问题就不难回答了。第一，适合的温度。如果温度太高，分子就会运动得太剧烈，无法结合成复杂的有机物；如果温度太低，分子的运动就会减慢甚至停止，同样无法形成复杂的有机物。第二，必要的水分。水是生命之源，一切生物都离不开水。第三，适当成分的大气。大气可以为生物提供呼吸所需要的氧气，同时还能保护生物不受过多紫外线辐射的伤害。第四，足够的光和热。这是生物进行光合作用获取能量的来源。到目前为止，地球是人类知道的唯一有生命存在的星球。但是宇宙是无限的，在遥远的星系中，很可能存在着其他有生命的星球。科学家们正在通过各种方式寻找地外生命的踪迹。也许在不久的将来，我们就能找到答案。", Difficulty.Normal),
        new("六年级·科普", "故宫博物院（黄传惕）", "在北京的中心，有一座城中之城，这就是紫禁城。现在人们叫它故宫，也叫故宫博物院。这是明清两代的皇宫，是我国现存的最大最完整的古代建筑群，已有五百多年的历史了。紫禁城的城墙十米多高，有四座城门：南面午门，北面神武门，东西面东华门、西华门。宫殿占地七十二万平方米，有大小宫殿七十多座、房屋九千多间。城墙外是五十多米宽的护城河。城墙的四角各有一座玲珑奇巧的角楼。故宫建筑群规模宏大壮丽，建筑精美，布局统一，集中体现了我国古代建筑艺术的独特风格。走进午门，是一个宽广的庭院，弯弯的金水河像一条玉带横贯东西，河上是五座精美的汉白玉石桥。桥的北面是太和门，一对威武的铜狮守卫在门的两侧。进了太和门，就来到紫禁城的中心三大殿：太和殿、中和殿、保和殿。太和殿俗称金銮殿，高二十八米，面积两千三百八十多平方米，是故宫最大的殿堂。在湛蓝的天空下，那金黄色的琉璃瓦重檐屋顶，显得格外辉煌。殿檐斗拱、额枋、梁柱，装饰着青蓝点金和贴金彩画。正面是十二根红色大圆柱，金琐窗，朱漆门，同台基相互衬映，色彩鲜明，雄伟壮丽。太和殿是举行重大典礼的地方。皇帝即位、生日、婚礼和元旦都在这里受朝贺。每逢大典，殿外的白石台基上下跪满文武大臣，中间御道两边排列着仪仗，皇帝端坐在金漆雕龙的宝座上。大殿廊下，鸣钟击磬，乐声悠扬。台基上的香炉和铜龟、铜鹤里点起檀香或松柏枝，烟雾缭绕。", Difficulty.Normal),

        new("六年级·成长", "我的伯父鲁迅先生（周晔）", "我的伯父鲁迅先生在世的时候，我年纪还小，只知道鲁迅的名字就吓得别人不敢接近，至于他是不是真的很可怕，我可不知道。记得有一次，伯父跟爸爸带我去看电影。我们走到电影院门口，看见一大堆人在围着一本书看。我们也凑了过去。那本书很厚，封面是黑色的。伯父问我：你喜欢看这本书吗？我点点头。伯父笑着说：那好，我给你买一本。我当时高兴得不得了。回到家里，伯父送了我好几本书，其中就有那本厚的黑色封面的书。后来我才知道，那本书叫《表》，是苏联作家写的儿童文学。伯父对我的学习非常关心。每次我去他家，他总要问问我的功课情况。如果我答得好，他就鼓励我继续努力；如果答得不好，他就会耐心地给我讲解。伯父对穷苦人也非常同情。有一次，我们在路上遇到一个拉黄包车的工人。那个工人的脚被玻璃割破了，血流不止。伯父赶紧跑过去，把他扶到一家诊所里包扎伤口，还给了他一些钱。事后伯父对我说：你要记住，做人要有同情心，要帮助那些需要帮助的人。伯父的话我一直铭记在心。一九三六年十月十九日，伯父去世了。那个时候我还很小，不太理解死亡意味着什么。但是现在回想起来，伯父虽然离开了我们，但他的精神永远活在我们心中。", Difficulty.Normal),

        new("六年级·爱国", "为人民服务（毛泽东）", "我们的共产党和共产党所领导的八路军、新四军，是革命的队伍。我们这个队伍完全是为着解放人民的，是彻底地为人民的利益工作的。张思德同志就是我们这个队伍中的一个同志。人总是要死的，但死的意义有不同。中国古时候有个文学家叫做司马迁的说过：人固有一死，或重于泰山，或轻于鸿毛。为人民利益而死，就比泰山还重；替法西斯卖力，替剥削人民和压迫人民的人去死，就比鸿毛还轻。张思德同志是为人民利益而死的，他的死是比泰山还要重的。因为我们是为人民服务的，所以我们如果有缺点，就不怕别人批评指出。不管是什么人，谁向我们指出都行。只要你说的对，我们就改正。你说的办法对人民有好处，我们就照你的办。精兵简政这一条意见，就是党外人士李鼎铭先生提出来的；他提得好，对人民有好处，我们就采用了。只要我们为人民的利益坚持好的，为人民的利益改正错的，我们这个队伍就一定会兴旺起来。我们都是来自五湖四海，为了一个共同的革命目标，走到一起来了。我们还要和全国大多数人民走这一条路。我们今天已经领导着有九千一百万人口的根据地，但是还不够，还要更大些，才能取得全民族的解放。我们的同志在困难的时候，要看到成绩，要看到光明，要提高我们的勇气。中国人民正在受难，我们有责任解救他们，我们要努力奋斗。要奋斗就会有牺牲，死人的事是经常发生的。但是我们想到人民的利益，想到大多数人民的痛苦，我们为人民而死，就是死得其所。不过，我们应当尽量地减少那些不必要的牺牲。我们的干部要关心每一个战士，一切革命队伍的人都要互相关心，互相爱护，互相帮助。", Difficulty.Hard),

        // ==================== 英文题库（小学3-6年级英语阅读风格）====================

        new("English·G3 Animals", "My Pet Dog", @"I have a wonderful pet dog named Max. He is a golden retriever with soft golden fur and big brown eyes. Max is three years old and weighs about thirty kilograms. He is very friendly and loves to play with everyone he meets.

Every morning, Max wakes me up by gently licking my face. He wants to go outside for his morning walk. I put on his leash and we walk around the park near our house for about twenty minutes. Max sniffs everything along the way – trees, bushes, fire hydrants, and other dogs' footprints. He especially likes chasing squirrels, though he never catches them!

After school, Max always waits for me at the door. As soon as he sees me, his tail wags so fast that his whole body wiggles. I give him a big hug and we play fetch in the backyard for an hour. Max can catch a tennis ball in mid-air and bring it back to me. He is really good at this game!

Max eats special dog food twice a day, but he also loves treats. His favorite snacks are dog biscuits and small pieces of cooked chicken. Once a week, my mom gives him a bath. He does not like baths much, but he stays still because he knows it is necessary.

Having a pet dog is a lot of responsibility. I have to feed him, walk him, play with him, and take care of him when he is sick. But Max gives me so much love and happiness in return. He is truly my best friend, and I cannot imagine life without him.", Difficulty.Easy),
        new("English·G3 Nature", "The Four Seasons", @"There are four seasons in a year, and each season is special in its own way. Let me tell you about what happens in each season.

Spring is the time when everything comes back to life. The snow melts away and green grass starts to grow. Trees grow new leaves and colorful flowers begin to bloom. You can see butterflies dancing among the flowers and hear birds singing happily in the branches. Baby animals are born in spring – you might see baby rabbits, ducklings, or even fawns in the forest. The weather gets warmer and the days become longer. Spring is a season of hope and new beginnings.

Summer is the hottest season of the year. The sun shines brightly in the blue sky. Children love summer because they have summer vacation from school. Many families go to the beach to swim and build sandcastles. Ice cream becomes everyone's favorite treat. In the countryside, farmers work hard in their fields because crops grow fast in the warm sunshine. Sometimes there are thunderstorms in the afternoon with loud thunder and bright lightning.

Autumn, also called fall, is when the leaves change color. Green leaves turn into beautiful shades of red, orange, yellow, and brown. Then they fall from the trees and cover the ground like a colorful carpet. The weather becomes cooler and we wear jackets and sweaters. Farmers harvest their crops – apples, pumpkins, corn, and many vegetables. Many animals gather food for the coming winter. Autumn is a beautiful and peaceful season.

Winter is the coldest season. In many places, snow falls and everything turns white. Children put on warm coats, gloves, hats, and scarves. They build snowmen, have snowball fights, and go sledding down snowy hills. Some animals hibernate, which means they sleep through the whole winter. Days are short and nights are long. Winter is a cozy time when families stay indoors, drink hot chocolate, and celebrate holidays together.

Each season brings its own beauty and activities. That is why I love all four seasons equally!", Difficulty.Easy),
        new("English·G3 School", "My Favorite Day at School", @"I go to school from Monday to Friday, but my favorite day is Wednesday. Let me tell you why Wednesday is so special for me.

On Wednesdays, my first class is English. I love learning English because our teacher, Ms. Lee, makes every lesson fun and interesting. She uses songs, games, and stories to teach us new words and grammar. Yesterday we learned colors by singing the Rainbow Song. Everyone laughed and sang along loudly. English class always puts me in a good mood for the rest of the day.

The second class is Math. I enjoy solving problems and learning about numbers, shapes, and patterns. On Wednesdays, we usually do group activities where we work together to solve puzzles. My group includes my best friends Tom and Lily, and we always try our best to finish first. Working in teams makes math much more fun than doing worksheets alone.

After two classes, we have a twenty-minute break. This is my favorite part of the morning! I buy a sandwich and an apple from the cafeteria and sit with my friends on the playground. We talk about our favorite cartoons, video games, and what we did last weekend. The break goes by too fast, but it gives me energy for the remaining classes.

In the afternoon, we have Art class on Wednesdays. Art is my absolute favorite subject! We paint, draw, make crafts, and learn about famous artists. Right now we are studying Vincent van Gogh and trying to paint our own starry nights. My painting is not as good as the original, but my teacher says I am improving every week.

Finally, the last class is PE – Physical Education. We play sports like basketball, soccer, and badminton. I am not the best athlete in my class, but I always try my best and have fun running around with my classmates. By the time school ends at four o'clock, I am tired but happy. Wednesday is indeed the best day of my school week!", Difficulty.Easy),
        new("English·G3 Daily Life", "A Busy Saturday Morning", @"Saturdays are busy mornings in my house. Everyone has chores to do, and we all work together to get everything finished before lunchtime.

I usually wake up at seven-thirty on Saturdays. The first thing I do is make my bed and tidy up my room. Mom says a clean room helps me think clearly and feel organized. Then I brush my teeth and wash my face. By eight o'clock, the delicious smell of breakfast fills the kitchen. Dad makes the best pancakes on Saturdays! He adds blueberries or chocolate chips, and we eat them with maple syrup and butter.

After breakfast, the real work begins. My chore is to vacuum the living room and dust the furniture. I use the vacuum cleaner to clean the carpet carefully, moving it back and forth until every part looks neat. Then I take a cloth and wipe the tables, TV stand, and bookshelves. Dust can hide everywhere, so I have to be thorough.

Meanwhile, Mom is busy in the kitchen. She washes the dishes from breakfast, cleans the refrigerator, and prepares ingredients for lunch and dinner. She also does laundry – sorting clothes by color, putting them in the washing machine, and then hanging them outside to dry in the sun.

Dad works in the garden. He waters the plants, pulls weeds, mows the lawn, and checks on the vegetables he is growing. This summer he planted tomatoes, cucumbers, and carrots. He says gardening is relaxing and rewarding because you can eat what you grow.

We usually finish all our chores before noon. Then the afternoon belongs to us. We might go to the park, visit grandparents, or just relax at home reading books and watching movies. Even though Saturday morning chores are tiring, it feels good to have a clean house and help my parents. Plus, knowing that the rest of the day is free makes the work seem easier!", Difficulty.Easy),

        new("English·G4 Science", "How Plants Grow", @"Have you ever wondered how a tiny seed becomes a tall tree or a beautiful flower? The process of plant growth is amazing and follows several important steps.

It all begins with a seed. A seed contains everything needed to start a new plant – a small supply of food and a tiny baby plant called an embryo. But seeds need the right conditions to start growing. They need water, warmth, oxygen, and usually some soil.

When a seed gets enough water, it absorbs the water and swells up. The seed coat cracks open, and the embryo inside begins to grow. The first thing to appear is the root, which grows downward into the soil. The root's job is to anchor the plant and absorb water and nutrients from the soil. Roots can be surprisingly long – some tree roots extend deeper than the tree is tall!

Next, the shoot grows upward toward the sunlight. At first, you might only see tiny pale sprouts. But soon, green leaves appear. These leaves are incredibly important because they contain chlorophyll, the green substance that allows plants to make their own food through photosynthesis. Using sunlight, water, and carbon dioxide from the air, leaves produce sugar that feeds the entire plant.

As the plant continues to grow, it develops a strong stem or trunk to support itself. The stem contains tubes called xylem and phloem that transport water and nutrients throughout the plant. Think of these tubes like the pipes in your house that carry water to different rooms.

Different plants grow at different speeds. A bean plant might grow from seed to maturity in just two months. An oak tree, however, takes decades to reach its full size. Some giant sequoia trees have been growing for over three thousand years and are still growing taller!

Understanding how plants grow helps us appreciate the green world around us. Every tree, flower, and blade of grass started as a tiny seed with incredible potential. Plants give us oxygen, food, medicine, materials for shelter, and beauty. Taking care of plants means taking care of ourselves and our planet.", Difficulty.Normal),
        new("English·G4 Culture", "Festivals Around the World", @"People all over the world celebrate festivals. Each festival has its own traditions, foods, and meanings. Learning about different festivals helps us understand and respect other cultures.

Chinese New Year, also called Spring Festival, is the most important festival in China. It usually falls in January or February. Families clean their houses thoroughly to sweep away bad luck. They decorate doors and windows with red paper cuttings and couplets written with good wishes. On New Year's Eve, families gather for a big reunion dinner with many special dishes like dumplings and fish. Children receive red envelopes with money inside, which brings good luck. Fireworks light up the sky at midnight to scare away evil spirits. The celebration lasts for fifteen days and ends with the Lantern Festival.

Thanksgiving is a major American holiday celebrated on the fourth Thursday in November. It originated from a harvest feast shared by Pilgrims and Native Americans in sixteen twenty-one. Today, families travel long distances to be together. The traditional meal includes roast turkey, mashed potatoes with gravy, cranberry sauce, and pumpkin pie. Before eating, many families share what they are thankful for. Watching football games and parades on television is also a popular Thanksgiving tradition.

Diwali, known as the Festival of Lights, is a major Hindu festival celebrated in India and other countries. It lasts for five days between October and November. People decorate their homes with rows of clay lamps called diyas. Beautiful rangoli designs made from colored powders adorn the entrances of homes. Families exchange gifts and sweets, wear new clothes, and set off fireworks. Diwali celebrates the victory of light over darkness and good over evil.

Eid al-Fitr is an important Muslim holiday that marks the end of Ramadan, the month of fasting. The date changes each year based on the lunar calendar. Muslims wake up early for special prayers, give charity to the poor, and celebrate with feasts featuring delicious traditional foods. Children receive new clothes and gifts. Visiting friends and relatives is an essential part of the celebration.

Although these festivals come from different cultures and religions, they all share common themes: family togetherness, sharing food, expressing gratitude, and hoping for a better future. Festivals remind us that despite our differences, people everywhere value similar things in life.", Difficulty.Normal),
        new("English·G4 Adventure", "A Day at the Zoo", @"Last weekend, my class went on a field trip to the city zoo. We had been looking forward to this trip for weeks, and finally the big day arrived! Everyone gathered at school at eight in the morning. Our teacher, Mr. Thompson, checked our names on the list, and then we boarded two big yellow buses. The ride took about forty minutes, and we spent the time singing songs and chatting excitedly about which animals we wanted to see first.

When we arrived at the zoo, we divided into small groups. My group included my best friend Emma, a boy named Ryan who knows everything about animals, and our parent volunteer, Sarah's mom. We got a map of the zoo and planned our route. There was so much to see – the map showed dozens of different exhibits!

Our first stop was the African Savanna exhibit. We saw giraffes with their incredibly long necks reaching up to eat leaves from tall trees. Ryan told us that a giraffe's tongue can be up to twenty inches long and is dark purple or blue! Nearby, zebras grazed peacefully. Their black and white stripes are unique to each individual, just like human fingerprints. We watched a lion sleeping in the shade – he looked so lazy, but our guide explained that lions sleep up to twenty hours a day to save energy for hunting.

Next, we visited the Tropical Rainforest building. It was warm and humid inside, just like a real rainforest. Colorful parrots flew overhead, calling out in loud voices. Tiny monkeys swung from branch to branch above our heads. In a glass enclosure, we saw a sloth moving very, very slowly. Did you know that sloths are so slow that algae actually grows on their fur? It looked like the sloto had a green coat!

At noon, we had a picnic lunch near the penguin exhibit. The penguins were hilarious – they waddled around awkwardly on land but swam gracefully in the water. One penguin kept sliding on its belly down a little hill, and all the children cheered and clapped.

Before leaving, we visited the Reptile House. I was nervous about seeing snakes, but they were actually quite interesting behind the glass. The biggest snake was a python that was longer than our school bus! It was coiled up sleeping, and I could see its scales shimmering under the light.

The bus ride home was much quieter than the morning trip – everyone was tired but happy. I fell asleep on Emma's shoulder and dreamed about all the amazing animals I had seen. This was definitely the best field trip ever!", Difficulty.Normal),
        new("English·G4 Health", "Staying Healthy", @"Being healthy is one of the most important things in life. When you are healthy, you have energy to play, learn, and enjoy each day. Here are some key ways to stay healthy.

First, eat nutritious food. Your body needs a balanced mix of different types of food. Fruits and vegetables provide vitamins and minerals that keep your body working properly. Try to eat at least five servings of fruits and vegetables every day. Whole grains like brown rice, oatmeal, and whole wheat bread give you lasting energy. Protein from meat, fish, eggs, beans, and nuts helps build strong muscles. Dairy products like milk and cheese strengthen your bones. Avoid too much sugar, salt, and processed food – these can make you feel tired and unhealthy.

Second, exercise regularly. Children should get at least one hour of physical activity every day. Exercise makes your heart stronger, builds muscles, improves your mood, and helps you sleep better at night. You do not need to play sports to exercise – walking, biking, swimming, dancing, and even playing tag with friends all count! Find activities that you enjoy so that exercising feels like fun, not work.

Third, get enough sleep. Sleep is when your body repairs itself and your brain organizes everything you learned during the day. Most children need between nine and eleven hours of sleep each night. Going to bed at the same time every night helps your body establish a healthy sleep routine. Avoid screens like phones and tablets before bedtime because the blue light can make it harder to fall asleep.

Fourth, drink plenty of water. Your body is mostly water, and you need to replace water that you lose through breathing, sweating, and going to the bathroom. Carry a water bottle with you and sip throughout the day. Water is healthier than sugary drinks like soda and juice.

Finally, wash your hands regularly. Germs spread easily from person to person, especially in schools where many children share spaces. Wash your hands with soap and warm water for at least twenty seconds before eating, after using the bathroom, and after coughing or sneezing. This simple habit can prevent many illnesses.

Taking care of your health is a lifelong habit. Start these good practices now while you are young, and they will benefit you for your entire life!", Difficulty.Normal),

        new("English·G5 History", "The Story of Paper and Printing", @"Imagine a world without books, newspapers, or computers. How would people share information and record history? For thousands of years, humans faced exactly this problem until some brilliant inventions changed everything. Two of the most important inventions came from China: paper and printing.

Before paper was invented, people wrote on many different materials. Ancient Egyptians wrote on papyrus made from reed plants. Babylonians pressed marks into wet clay tablets that were then dried in the sun. Ancient Chinese wrote on animal bones, turtle shells, strips of bamboo, and pieces of silk. But all of these materials had problems – they were either too heavy, too expensive, or too difficult to make.

Around the year one hundred and five AD, a Chinese official named Cai Lun invented paper as we know it. He mixed mulberry tree bark, hemp, old rags, and fishnets with water, pounded them into a pulp, and then spread the mixture thinly on a screen to dry. The result was a thin, flexible, and inexpensive material perfect for writing. Papermaking gradually spread from China to the Arab world, then to Europe, and eventually around the globe. Without paper, human civilization would have developed much more slowly.

Even more revolutionary was the invention of movable type printing. Before this, all books had to be copied by hand, which was extremely slow and expensive. Very few people owned books, and knowledge was limited to a small elite. Around the year one thousand and forty, a Chinese artisan named Bi Sheng invented characters made of baked clay that could be arranged in any order, inked, and pressed onto paper to create pages of text. This meant that the same characters could be reused over and over again to print different books.

Later, Johannes Gutenberg in Germany improved the system using metal letters and a printing press. His Gutenberg Bible, printed around fourteen fifty, was the first major book produced with movable type in Europe. This invention sparked an information revolution. Books became cheaper and more available. Knowledge spread rapidly. Literacy rates increased. Science, religion, and art all advanced because ideas could be shared widely.

Today, we take paper and printed words for granted. But remember that for most of human history, these simple things did not exist. The inventions of paper and printing are among the most important contributions to human progress, enabling education, communication, and the preservation of knowledge across generations.", Difficulty.Normal),
        new("English·G5 Environment", "Protecting Our Oceans", @"Oceans cover more than seventy percent of Earth's surface, yet they remain one of the most mysterious and threatened parts of our planet. Our oceans are in trouble, and understanding why is the first step toward protecting them.

One major problem is plastic pollution. Every year, millions of tons of plastic waste enter the oceans. Plastic bottles, bags, straws, fishing nets, and microplastics (tiny plastic particles) float in the water or sink to the bottom. Marine animals mistake plastic for food. Sea turtles eat plastic bags thinking they are jellyfish. Fish consume microplastics that then enter the food chain – and eventually end up on our dinner plates. Plastic takes hundreds of years to break down, meaning every piece of plastic ever made still exists somewhere unless it was properly recycled.

Another serious issue is overfishing. Modern fishing boats use huge nets and advanced technology that can locate and capture enormous numbers of fish. Many fish populations have declined dramatically. Some species, like the bluefin tuna, are endangered. When too many fish are removed from the ocean, the entire ecosystem suffers. Seabirds that depend on fish for food starve. Coral reefs that rely on certain fish to stay healthy begin to die. The balance of marine life is disrupted.

Ocean temperatures are rising due to climate change. Warmer water causes coral bleaching – when corals get stressed, they expel the colorful algae living in their tissues and turn white. If the water stays too warm, the corals die. Rising temperatures also cause sea levels to expand and polar ice to melt, threatening coastal communities worldwide. Ocean acidification occurs when seawater absorbs excess carbon dioxide from the atmosphere, making the water more acidic. This makes it harder for shellfish, corals, and some plankton to build their shells and skeletons.

What can young people do to help? Reduce single-use plastics by carrying reusable bags, bottles, and containers. Choose sustainable seafood – ask adults to check if fish was caught responsibly. Save energy at home to reduce carbon emissions. Participate in beach cleanups. Learn more about marine biology and share knowledge with friends and family. Every small action adds up when millions of people work together. Our oceans gave life to this planet; now it is our turn to protect them.", Difficulty.Normal),
        new("English·G5 Technology", "How Computers Work", @"Computers are everywhere – in our homes, schools, phones, cars, and even refrigerators! But have you ever wondered what is actually happening inside that box on your desk? Let us explore the amazing inner workings of a computer.

At its core, a computer is a machine that processes information. It consists of several main parts working together. The central processing unit, or CPU, is often called the brain of the computer. It performs calculations and executes instructions at incredible speeds – billions of operations per second! The CPU reads instructions from memory, processes data, and sends results to output devices.

Random Access Memory, or RAM, is the computer's short-term memory. When you open a program or document, it loads into RAM so the CPU can access it quickly. RAM is fast but temporary – when you turn off the computer, everything in RAM disappears. That is why you need to save your work to storage.

Storage devices like hard drives and solid-state drives provide long-term memory. Unlike RAM, storage keeps your files safe even when the power is off. Your operating system, applications, documents, photos, videos, and games all live here. Modern solid-state drives are much faster than old mechanical hard drives because they have no moving parts.

The motherboard is a large circuit board that connects all the components together. Think of it as the nervous system of the computer. Wires and circuits on the motherboard allow electricity and data to flow between the CPU, RAM, storage, and other parts. Expansion slots on the motherboard let you add extra components like graphics cards for better gaming performance.

Input devices let you communicate with the computer. Keyboards and mice are the most common input devices, but microphones, webcams, touchscreens, and game controllers also send information into the computer. Output devices show you the results – monitors display images and text, speakers play sound, and printers create paper copies.

Software is the set of instructions that tells the hardware what to do. The operating system manages all the basic functions and lets you run applications. Applications (also called apps or programs) are specific tools for tasks like writing essays, browsing the internet, editing photos, or playing games.

Computers understand only one language: binary code, which uses only zeros and ones. Everything you see on the screen – text, images, videos, games – is represented by long strings of zeros and ones deep inside the computer. Programmers write code in languages like Python, Java, or Scratch, and special programs translate this code into binary that the computer can execute.

Understanding how computers work helps you use them more effectively and maybe even inspires you to create your own programs someday. Who knows – you might design the next groundbreaking technology that changes the world!", Difficulty.Normal),

        new("English·G6 Literature", "The Gift of the Magi (Adapted)", @"One dollar and eighty-seven cents. That was all Della had. And tomorrow was Christmas. She counted the money three times. One dollar and eighty-seven cents. And the next day would be Christmas. There was clearly nothing left to do but flop down on the shabby little couch and cry. So Della did it.

Della and Jim Young were a poor but loving married couple. They lived in a modest apartment in New York City. Both Della and Jim had one precious possession each. Jim owned a gold watch that had belonged to his father and grandfather. It was a beautiful watch, but Jim kept it wrapped in an old leather strap because he could not afford a proper chain. Della had beautiful long hair that flowed down her back like a cascade of brown waters. It was more beautiful than any queen's jewels.

On Christmas Eve, Della stood before the mirror, tears in her eyes. She wanted to buy Jim a Christmas gift, but one dollar and eighty-seven cents was nowhere near enough for a proper gold watch chain. Suddenly, she had an idea. She put on her old brown coat and hurried out into the cold winter night. She went to a shop that bought hair and sold the shopkeeper her beautiful long hair for twenty dollars.

With the twenty dollars plus her one dollar and eighty-seven cents, Della searched the stores until she found the perfect platinum watch chain. It was simple and elegant, worthy of Jim's precious gold watch. She paid twenty-one dollars and hurried home, excited to surprise Jim.

When Jim came home and saw Della's short hair, he was shocked. His expression was one that Della could not read. Then Jim smiled and handed her a package. Inside lay the combs – the beautiful tortoiseshell combs with jeweled rims that Della had admired in a shop window for months. They were perfect for her long flowing hair – the hair she had just sold.

Della burst into tears, then remembered and held up her gift – the platinum chain. Jim sat down on the couch and smiled. He sold his gold watch to buy those combs for her. Now Della had combs but no hair, and Jim had a chain but no watch. Yet as they sat together on their small couch eating a humble dinner, they realized something important: their gifts were useless in a practical sense, but they proved something far more valuable – the depth of their love for each other. The author O. Henry called them foolish children who sacrificed their greatest treasures for each other, but perhaps they were the wisest of all.", Difficulty.Hard),
        new("English·G6 Science", "The Mystery of Black Holes", @"Imagine a place in space where gravity is so powerful that nothing can escape – not even light. This is not science fiction; it is a real phenomenon called a black hole. Black holes are among the most fascinating and mysterious objects in our universe.

To understand black holes, we need to think about gravity in a new way. Gravity is the force that pulls things toward each other. The more massive something is, the stronger its pull. Earth's gravity keeps you, your toys, and the air stuck to the ground. The Sun's gravity keeps Earth and all the other planets orbiting around it. Now imagine squeezing the Sun until it was only a few kilometers across. Its mass would be the same, but packed into a tiny space. The gravity at its surface would become unimaginably strong. Strong enough that even light, which travels faster than anything else in the universe, could not escape. That is a black hole.

Black holes form when massive stars die. Stars are like giant nuclear reactors, burning fuel in their cores for millions or billions of years. Eventually, a star runs out of fuel. Without the outward pressure from nuclear reactions balancing the inward pull of gravity, the star collapses. If the star is massive enough – about twenty times heavier than our Sun – nothing can stop the collapse. The star keeps shrinking and shrinking until it becomes infinitely dense, crushed to a point called a singularity. Around this singularity is a boundary called the event horizon. Once anything crosses the event horizon, including light, it can never return.

Scientists cannot observe black holes directly because no light escapes from them. However, they can detect black holes by their effects on nearby matter. When a black hole pulls in gas and dust from nearby stars, this material heats up tremendously before disappearing beyond the event horizon. The superheated material glows brightly in X-rays that telescopes can detect. Also, black holes are so massive that they bend the light from stars behind them, creating a visible distortion effect called gravitational lensing.

In twenty nineteen, scientists made history by capturing the first-ever image of a black hole. Using a network of radio telescopes around the world called the Event Horizon Telescope, they photographed the supermassive black hole at the center of galaxy M87. The image showed a dark circle surrounded by a ring of bright light – exactly what Einstein's theory of general relativity predicted a century earlier.

Black holes range in size. Stellar black holes form from individual dying stars and can be ten to twenty times the mass of our Sun. Supermassive black holes, found at the centers of galaxies, contain millions or even billions of solar masses. The black hole at the center of our own Milky Way galaxy, called Sagittarius A*, is about four million times as massive as our Sun.

Despite their fearsome reputation, black holes are not dangerous monsters roaming space eating everything. They follow the laws of physics just like everything else. Unless you get very close to one, you would not notice anything unusual. Black holes are simply nature's most extreme objects, pushing the boundaries of what we thought possible in our universe.", Difficulty.Hard)
    };

    /// <summary>返回全部内置题库文章（供练习页下拉框列举）。</summary>
    public static IReadOnlyList<BuiltInArticle> GetBuiltInArticles() => BuiltInArticlesList;

    /// <summary>按难度随机取一篇中文题库正文；该难度无中文文章时退回全中文库随机，再退回全库。</summary>
    public static string GetRandomBuiltIn(Difficulty difficulty)
    {
        var cn = BuiltInArticlesList.FindAll(a => IsChineseBody(a.Body));
        var pool = cn.FindAll(a => a.Difficulty == difficulty);
        if (pool.Count == 0) pool = cn;
        if (pool.Count == 0) pool = BuiltInArticlesList;
        return pool[System.Random.Shared.Next(pool.Count)].Body;
    }

    /// <summary>正文是否以中文为主（含 CJK 字符）。</summary>
    private static bool IsChineseBody(string s)
        => !string.IsNullOrEmpty(s) && s.Any(c => c is >= (char)0x4E00 and <= (char)0x9FFF);

    public static IReadOnlyList<string> GetTexts(PracticeType type, Difficulty difficulty)
    {
        return type switch
        {
            PracticeType.Chinese or PracticeType.ChineseWord => ChineseTexts[difficulty],
            PracticeType.SpeedTest => SpeedTexts,
            _ => EnglishTexts[difficulty]
        };
    }

    public static string GetRandomText(PracticeType type, Difficulty difficulty)
    {
        var list = GetTexts(type, difficulty).ToList();
        if (list.Count == 0) return string.Empty;
        int idx = System.Random.Shared.Next(list.Count);
        return list[idx];
    }

    #region 难度细分（1~10 级）

    /// <summary>
    /// 1~10 级细分难度对应的目标文本长度（字符数），等级越高文本越长。
    ///
    /// 历史说明：旧值为 { 40, 70, 110, ... }，等级 1 仅 40 字符 ≈ 一行，
    /// 用户打完第一行就没有后续内容可打（"停在一行不继续"）。
    /// 现整体加长约一倍：最低等级也有约两行，高等级足够铺满一屏，
    /// 配合 <c>TypingViewModel</c> 的自动续接，练习可以一直进行下去。
    /// </summary>
    private static readonly int[] LevelTargetLengths = { 80, 140, 200, 280, 360, 450, 550, 660, 800, 950 };

    /// <summary>
    /// 按细分等级获取目标文本长度（字符数）。
    /// </summary>
    /// <param name="level">细分等级（1~10，越界自动收敛）</param>
    /// <returns>该等级对应的目标字符数</returns>
    public static int GetTargetLength(int level)
    {
        int index = DifficultyScale.Clamp(level) - DifficultyScale.Min;
        if (index < 0 || index >= LevelTargetLengths.Length)
        {
            return LevelTargetLengths[^1];
        }
        return LevelTargetLengths[index];
    }

    /// <summary>
    /// 按练习类型 + 难度 + 细分等级随机取文：细分等级决定文本长度。
    /// 限时测速会拼接多条不同的测速文本，其余类型在单篇基础上续接或截断。
    /// </summary>
    /// <param name="type">练习类型</param>
    /// <param name="difficulty">三档难度</param>
    /// <param name="level">细分等级（1~10）</param>
    /// <returns>按细分等级调整过长度的对照文本</returns>
    public static string GetRandomText(PracticeType type, Difficulty difficulty, int level)
    {
        int target = GetTargetLength(level);

        // 单词 / 词组练习由 WordLibrary 提供分级词表，与文章级练习分工不同
        if (type == PracticeType.EnglishWord)
        {
            return WordLibrary.GetEnglishWordText(difficulty, level);
        }
        if (type == PracticeType.ChineseWord)
        {
            return WordLibrary.GetChineseWordText(difficulty, level);
        }

        if (type == PracticeType.SpeedTest)
        {
            return BuildSpeedTestText(target);
        }

        // 普通练习：取一篇后按需拼接同类型的其它文本，避免重复同一段
        string head = GetRandomText(type, difficulty);
        var same = GetTexts(type, difficulty).Where(s => s != head).ToList();
        return FitLength(head, target, same);
    }

    /// <summary>
    /// 按难度 + 细分等级随机取一篇内置中文文章：等级越高，截取的正文越长。
    /// </summary>
    /// <param name="difficulty">三档难度</param>
    /// <param name="level">细分等级（1~10）</param>
    /// <returns>按细分等级调整过长度的中文正文</returns>
    public static string GetRandomBuiltIn(Difficulty difficulty, int level)
    {
        string first = GetRandomBuiltIn(difficulty);
        // 同难度中文文章池（去掉第一篇），用于长度不足时续接不同内容
        var cn = BuiltInArticlesList.FindAll(a => IsChineseBody(a.Body));
        var pool = cn.FindAll(a => a.Difficulty == difficulty);
        if (pool.Count == 0) pool = cn;
        var others = pool.Select(a => a.Body).Where(b => b != first).ToList();
        return FitLength(first, GetTargetLength(level), others);
    }

    /// <summary>
    /// 把文本调整到目标长度：不足则循环续接，超出则按自然断点截断。
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <param name="targetLength">目标字符数</param>
    /// <returns>长度接近目标值的文本</returns>
    private static string FitLength(string text, int targetLength)
        => FitLength(text, targetLength, null);

    /// <summary>
    /// 把文本调整到目标长度：超出则按自然断点截断，不足则**拼接其它文章**（而非重复本文）。
    ///
    /// 为什么要拼接不同文章：旧逻辑在长度不足时把同一篇再拼一份，
    /// 实测会产出「太阳当空照…（若干字）…太阳当空照…」这种肉眼可见的重复，
    /// 既影响观感，也让用户反复打同一段。改为从候选池里抽【不同】的文章续接。
    /// </summary>
    /// <param name="text">第一篇文章正文</param>
    /// <param name="targetLength">目标字符数</param>
    /// <param name="morePool">可续接的其它文章正文；为 null 时退化为仅截断</param>
    /// <returns>长度接近目标值的文本</returns>
    private static string FitLength(string text, int targetLength, IReadOnlyList<string>? morePool)
    {
        if (string.IsNullOrWhiteSpace(text) || targetLength <= 0)
        {
            return text ?? string.Empty;
        }
        if (text.Length >= targetLength)
        {
            return CutAtBreak(text, targetLength);
        }

        var builder = new StringBuilder(text);

        if (morePool is null || morePool.Count == 0)
        {
            // 没有其它素材可用：只能重复本篇（保底，避免返回空文本）
            while (builder.Length < targetLength)
            {
                builder.Append(text);
            }
            return CutAtBreak(builder.ToString(), targetLength);
        }

        // 把候选池打乱后依次续接，尽量不复用同一篇
        var shuffled = morePool.OrderBy(_ => Random.Shared.Next()).ToList();
        int idx = 0;
        int guard = 0;
        while (builder.Length < targetLength && guard++ < 200)
        {
            string next = shuffled[idx % shuffled.Count];
            idx++;
            // 跳过与当前尾巴相同的片段，避免出现相邻重复
            if (next == text && idx == 1)
            {
                continue;
            }
            builder.Append(next);
        }
        return CutAtBreak(builder.ToString(), targetLength);
    }

    /// <summary>
    /// 限时测速：随机拼接若干条不同的测速文本，直到达到目标长度。
    /// </summary>
    /// <param name="targetLength">目标字符数</param>
    /// <returns>拼接后的测速文本</returns>
    private static string BuildSpeedTestText(int targetLength)
    {
        if (SpeedTexts.Count == 0) return string.Empty;

        var builder = new StringBuilder();
        var pool = SpeedTexts.OrderBy(_ => System.Random.Shared.Next()).ToList();
        int index = 0;
        while (builder.Length < targetLength)
        {
            builder.Append(pool[index % pool.Count]);
            builder.Append(' ');
            index++;
        }
        return CutAtBreak(builder.ToString(), targetLength);
    }

    /// <summary>
    /// 在不超过 maxLength 的前提下，按最近的句子断点截断文本，避免把单词或句子切碎。
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <param name="maxLength">允许的最大字符数</param>
    /// <returns>截断后的文本</returns>
    private static string CutAtBreak(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }
        string head = text[..maxLength];
        char[] breaks = { '。', '！', '？', '；', '.', '!', '?', ';', '，', ',', ' ' };
        int cut = head.LastIndexOfAny(breaks);
        // 断点太靠前时宁可硬切，保证文本长度接近目标值
        return cut >= maxLength / 2 ? head[..(cut + 1)] : head;
    }

    #endregion 难度细分（1~10 级）

    /// <summary>
    /// 打字小游戏单词池：改由 <see cref="GameWords"/> 提供（共 4700 词）。
    ///
    /// 历史说明：早期版本在这里硬编码了 66 个词（简单 24 / 普通 22 / 困难 20），
    /// 实测连抽 20 次只有 14 个不同词，玩家会明显感到"翻来覆去就这几个"。
    /// 现改为读取按词频分级的大词表，并保持方法签名不变，调用方无需改动。
    /// </summary>
    /// <param name="difficulty">三档难度</param>
    /// <returns>该难度对应的单词池</returns>
    public static IReadOnlyList<string> GetGameWords(Difficulty difficulty)
        => GameWords.ForDifficulty(difficulty);
}
