/**
 * Creates the NavMenu instance and returns it
 * @returns {NavMenu} the navmenu instance
 */
export function createNavMenu()
{
    return new NavMenu();
}

/**
 * NavMenu JavaScript file
 */
export class NavMenu {
    
    /**
     * Constructs the NavMenu instance
     */
    constructor()
    {
        this.ul = document.getElementById('ul-nav-menu');
        if(this.ul) {
            this.resizeMenu = this.resizeMenu.bind(this); // Bind resizeMenu to the class instance
            //new ResizeObserver(this.resizeMenu).observe(this.ul)

            window.addEventListener('resize', this.resizeMenu);
        }

        // Get the computed style of the root element
        const htmlComputedStyle = window.getComputedStyle(document.documentElement);
        const rootFontSize = htmlComputedStyle.getPropertyValue('font-size');
        this.rem  = parseFloat(rootFontSize);
        this.styleSheet = document.createElement('style');
        document.head.appendChild(this.styleSheet);
    }

    setCSS(css) {
        this.styleSheet.innerText = '';
        this.styleSheet.innerText = '@media (min-width:850px) { \n' + css + '\n}';
    }

    menuSet(groups, totalItems, collapsed)
    {
        this.groups = groups;
        this.totalItems = totalItems;
        this.collapsed = collapsed;
        this.resizeMenu();
    }
    
    /**
     * Resizes the menu 
     */
    resizeMenu(){
        if(!this.groups || !this.totalItems)
            return;

        let maxHeight = this.ul.clientHeight;
        let items = this.totalItems - (this.collapsed ? 0 : this.groups) + 1;

        let forcedNoGroups = this.totalItems > 16 ? maxHeight < 600 :
            this.totalItems > 13 ? 580 :
            this.totalItems > 10 ? 550 :
            this.totalItems > 6 ? 500 :
            300;
        
        let idealGroupHeight = forcedNoGroups ? 0 : this.collapsed ? 0 : 2.75;
        let idealItemHeight = this.collapsed ? 3 : 2.5;
                
        let height = (idealGroupHeight * this.groups * this.rem) + (idealItemHeight * items * this.rem);
        if(height <= maxHeight)
        {
            this.setHeights(idealGroupHeight, idealItemHeight);
            return;
        }
        
        let percent = (height - maxHeight) / maxHeight * 100;
        let groupHeight = forcedNoGroups ? 0 : this.collapsed ? 0 : percent < 25 ? 2 : 0;
        
        let itemHeight = idealItemHeight + 0.1;
        while(height > maxHeight && itemHeight > 1.5)
        {
            itemHeight -= 0.1;
            height = (idealGroupHeight * this.groups * this.rem) + (itemHeight * items * this.rem);
        }
        this.setHeights(groupHeight, itemHeight);
    }
    
    setHeights(groupHeight, itemHeight){
        let css = '';
        if(!groupHeight)
            css += '.nav-menu-group { display: none !important; }\n';
        else
            css += `.nav-menu-group { padding-bottom:0.25rem; height: ${groupHeight - 0.25}rem !important; }\n`;
        
        css += `.nav-item { height: ${itemHeight - 0.1}rem !important; padding:0.05rem 0; }`;
        css += `.nav-menu-container.collapse .nav-item { height: ${itemHeight + 0.25}rem !important; }`;
        this.setCSS(css);
    }
}