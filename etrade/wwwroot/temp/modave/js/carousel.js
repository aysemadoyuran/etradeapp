if ($(".tf-sw-top_bar").length > 0) {
  var tfSwTopbar = $(".tf-sw-top_bar");
  var preview = tfSwTopbar.data("preview");
  var spacing = tfSwTopbar.data("space");
  var loop = tfSwTopbar.data("loop");
  var speed = tfSwTopbar.data("speed");
  var play = tfSwTopbar.data("auto-play");
  var delay = tfSwTopbar.data("delay");
  var swiper = new Swiper(".tf-sw-top_bar", {
    autoplay: {
      delay: delay,
      disableOnInteraction: false,
      pauseOnMouseEnter: true,
    },
    observer: true,
    observeParents: true,
    autoplay: play,
    slidesPerView: preview,
    loop: loop,
    spaceBetween: spacing,
    speed: speed,
    navigation: {
      clickable: true,
      nextEl: ".nav-prev-topbar",
      prevEl: ".nav-next-topbar",
    },
  });

  tfSwTopbar.hover(
    function () {
      this.swiper.autoplay.stop();
    },
    function () {
      this.swiper.autoplay.start();
    }
  );
}

if ($(".tf-sw-slideshow").length > 0) {
  var tfSwSlideshow = $(".tf-sw-slideshow");
  var preview = tfSwSlideshow.data("preview");
  var tablet = tfSwSlideshow.data("tablet");
  var mobile = tfSwSlideshow.data("mobile");
  var spacing = tfSwSlideshow.data("space");
  var spacingMb = tfSwSlideshow.data("space-mb");
  var loop = tfSwSlideshow.data("loop");
  var play = tfSwSlideshow.data("auto-play");
  var centered = tfSwSlideshow.data("centered");
  var effect = tfSwSlideshow.data("effect");
  var speed = tfSwSlideshow.data("speed") !== undefined ? tfSwSlideshow.data("speed") : 1000;
  var swiperSlider =  {
    autoplay: {
      disableOnInteraction: false,
      pauseOnMouseEnter: true,
    },
    autoplay: play,
    slidesPerView: mobile,
    loop: loop,
    spaceBetween: spacingMb,
    speed: speed,
    observer: true,
    observeParents: true,
    pagination: {
      el: ".sw-pagination-slider",
      clickable: true,
    },
    navigation: {
      clickable: true,
      nextEl: ".navigation-next-slider",
      prevEl: ".navigation-prev-slider",
    },
    centeredSlides: false,
    breakpoints: {
      768: {
        slidesPerView: tablet,
        spaceBetween: spacing,
        centeredSlides: false,

      },
      1200: {
        slidesPerView: preview,
        spaceBetween: spacing,
        centeredSlides: centered,
      },
    },
  };
  if (effect === 'fade') {
    swiperSlider.effect = 'fade';
    swiperSlider.fadeEffect = {
      crossFade: true,
    };
  }
  
  var swiper = new Swiper(".tf-sw-slideshow", swiperSlider);
}

if ($(".tf-sw-effect").length > 0) {
  var swiper2 = new Swiper(".tf-sw-effect", {
    spaceBetween: 0,
    speed: 2000,
    effect: "fade",
    observer: true,
    observeParents: true,
    fadeEffect: {
      crossFade: true,
    },
    pagination: {
      el: ".sw-pagination-slider",
      clickable: true,
    },
    navigation: {
      clickable: true,
      nextEl: ".nav-next-slider",
      prevEl: ".nav-prev-slider",
    },
  });
}

if ($(".tf-sw-collection").length > 0) {
  var tfSwCls = $(".tf-sw-collection");
  var preview = tfSwCls.data("preview");
  var tablet = tfSwCls.data("tablet");
  var mobile = tfSwCls.data("mobile");
  var mobileSm = tfSwCls.data("mobile-sm") !== undefined ? tfSwCls.data("mobile-sm") : mobile;
  var spacingLg = tfSwCls.data("space-lg");
  var spacingMd = tfSwCls.data("space-md");
  var spacing = tfSwCls.data("space");
  var loop = tfSwCls.data("loop");
  var perGroup = tfSwCls.data("pagination") || 1;
  var perGroupMd = tfSwCls.data("pagination-md") || 1;
  var perGroupLg = tfSwCls.data("pagination-lg") || 1;
  var swiper = new Swiper(".tf-sw-collection", {
    slidesPerView: mobile,
    spaceBetween: spacing,
    speed: 1000,
    pagination: {
      el: ".sw-pagination-collection",
      clickable: true,
    },
    observer: true,
    observeParents: true,
    slidesPerGroup: perGroup,
    navigation: {
      clickable: true,
      nextEl: ".nav-next-collection",
      prevEl: ".nav-prev-collection",
    },
    breakpoints: {
      575: {
        slidesPerView: mobileSm,
        spaceBetween: spacing,
        slidesPerGroup: perGroup,
      },
      768: {
        slidesPerView: tablet,
        spaceBetween: spacingMd,
        slidesPerGroup: perGroupMd,
      },
      1200: {
        slidesPerView: preview,
        spaceBetween: spacingLg,
        slidesPerGroup: perGroupLg,
      },
    },
  });
}

if ($(".tf-sw-categories").length > 0) {
  var tfSwCategories = $(".tf-sw-categories");
  var preview = tfSwCategories.data("preview");
  var tablet = tfSwCategories.data("tablet");
  var mobile = tfSwCategories.data("mobile");
  var mobileSm = tfSwCategories.data("mobile-sm") !== undefined ? tfSwCategories.data("mobile-sm") : mobile;
  var spacingLg = tfSwCategories.data("space-lg");
  var spacingMd = tfSwCategories.data("space-md");
  var spacing = tfSwCategories.data("space");
  var perGroup = tfSwCategories.data("pagination") || 1;
  var perGroupMd = tfSwCategories.data("pagination-md") || 1;
  var perGroupLg = tfSwCategories.data("pagination-lg") || 1;
  var loop = tfSwCategories.data("loop") !== undefined ?  tfSwCategories.data("loop") : false;
  var centered = tfSwCategories.data("centered") !== undefined ? tfSwCategories.data("centered") : false;
  var swiper = new Swiper(".tf-sw-categories", {
    slidesPerView: mobile,
    spaceBetween: spacing,
    speed: 1000,
    pagination: {
      el: ".sw-pagination-categories",
      clickable: true,
    },
    slidesPerGroup: perGroup,
    observer: true,
    centeredSlides: centered,
    observeParents: true,
    navigation: {
      clickable: true,
      nextEl: ".nav-next-categories",
      prevEl: ".nav-prev-categories",
    },
    loop:loop,
    breakpoints: {
      575: {
        slidesPerView: mobileSm,
        spaceBetween: spacing,
        slidesPerGroup: perGroup,
      },
      768: {
        slidesPerView: tablet,
        spaceBetween: spacingMd,
        slidesPerGroup: perGroupMd,
      },
      1200: {
        slidesPerView: preview,
        spaceBetween: spacingLg,
        slidesPerGroup: perGroupLg,
      },
    },
  });
}

if ($(".tf-sw-recent").length > 0) {
  var preview = $(".tf-sw-recent").data("preview");
  var tablet = $(".tf-sw-recent").data("tablet");
  var mobile = $(".tf-sw-recent").data("mobile");
  var spacingLg = $(".tf-sw-recent").data("space-lg");
  var spacingMd = $(".tf-sw-recent").data("space-md");
  var spacing = $(".tf-sw-recent").data("space");
  var perGroup = $(".tf-sw-recent").data("pagination");
  var perGroupMd = $(".tf-sw-recent").data("pagination-md");
  var perGroupLg = $(".tf-sw-recent").data("pagination-lg");
  var swiper = new Swiper(".tf-sw-recent", {
    slidesPerView: mobile,
    spaceBetween: spacing,
    pagination: {
      el: ".sw-pagination-recent",
      clickable: true,
    },
    slidesPerGroup: perGroup,
    observer: true,
    observeParents: true,
    speed: 1000,
    navigation: {
      clickable: true,
      nextEl: ".nav-prev-recent",
      prevEl: ".nav-next-recent",
    },
    breakpoints: {
      768: {
        slidesPerView: tablet,
        spaceBetween: spacingMd,
        slidesPerGroup: perGroupMd,
      },
      1200: {
        slidesPerView: preview,
        spaceBetween: spacingLg,
        slidesPerGroup: perGroupLg,
      },
    },
  });
}

if ($(".tf-sw-latest").length > 0) {
  var preview = $(".tf-sw-latest").data("preview");
  var tablet = $(".tf-sw-latest").data("tablet");
  var mobile = $(".tf-sw-latest").data("mobile");
  var spacingLg = $(".tf-sw-latest").data("space-lg");
  var spacingMd = $(".tf-sw-latest").data("space-md");
  var spacing = $(".tf-sw-latest").data("space");
  var perGroup = $(".tf-sw-latest").data("pagination");
  var perGroupMd = $(".tf-sw-latest").data("pagination-md");
  var perGroupLg = $(".tf-sw-latest").data("pagination-lg");
  var swiper = new Swiper(".tf-sw-latest", {
    slidesPerView: mobile,
    spaceBetween: spacing,
    observer: true,
    observeParents: true,
    speed: 1000,
    pagination: {
      el: ".sw-pagination-latest",
      clickable: true,
    },
    slidesPerGroup: perGroup,
    navigation: {
      clickable: true,
      nextEl: ".nav-prev-latest",
      prevEl: ".nav-next-latest",
    },
    breakpoints: {
      768: {
        slidesPerView: tablet,
        spaceBetween: spacingMd,
        slidesPerGroup: perGroupMd,
      },
      1200: {
        slidesPerView: preview,
        spaceBetween: spacingLg,
        slidesPerGroup: perGroupLg,
      },
    },
  });
}

if ($(".tf-sw-products").length > 0) {
  var preview = $(".tf-sw-products").data("preview");
  var tablet = $(".tf-sw-products").data("tablet");
  var mobile = $(".tf-sw-products").data("mobile");
  var spacingLg = $(".tf-sw-products").data("space-lg");
  var spacingMd = $(".tf-sw-products").data("space-md");
  var spacing = $(".tf-sw-products").data("space");
  var perGroup = $(".tf-sw-products").data("pagination");
  var perGroupMd = $(".tf-sw-products").data("pagination-md");
  var perGroupLg = $(".tf-sw-products").data("pagination-lg");
  var swiper = new Swiper(".tf-sw-products", {
    speed: 1000,
    slidesPerView: mobile,
    spaceBetween: spacing,
    pagination: {
      el: ".sw-pagination-products",
      clickable: true,
    },
    slidesPerGroup: perGroup,
    observer: true,
    observeParents: true,
    navigation: {
      clickable: true,
      nextEl: ".nav-prev-products",
      prevEl: ".nav-next-products",
    },
    breakpoints: {
      768: {
        slidesPerView: tablet,
        spaceBetween: spacingMd,
        slidesPerGroup: perGroupMd,
      },
      1200: {
        slidesPerView: preview,
        spaceBetween: spacingLg,
        slidesPerGroup: perGroupLg,
      },
    },
  });
}

if ($(".tf-sw-products1").length > 0) {
  var preview = $(".tf-sw-products1").data("preview");
  var tablet = $(".tf-sw-products1").data("tablet");
  var mobile = $(".tf-sw-products1").data("mobile");
  var spacingLg = $(".tf-sw-products1").data("space-lg");
  var spacingMd = $(".tf-sw-products1").data("space-md");
  var spacing = $(".tf-sw-products1").data("space");
  var perGroup = $(".tf-sw-products1").data("pagination");
  var perGroupMd = $(".tf-sw-products1").data("pagination-md");
  var perGroupLg = $(".tf-sw-products1").data("pagination-lg");
  var swiper = new Swiper(".tf-sw-products1", {
    slidesPerView: mobile,
    spaceBetween: spacing,
    observer: true,
    observeParents: true,
    pagination: {
      el: ".sw-pagination-products1",
      clickable: true,
    },
    speed: 1000,
    slidesPerGroup: perGroup,
    navigation: {
      clickable: true,
      nextEl: ".nav-prev-products1",
      prevEl: ".nav-next-products1",
    },
    breakpoints: {
      768: {
        slidesPerView: tablet,
        spaceBetween: spacingMd,
        slidesPerGroup: perGroupMd,
      },
      1200: {
        slidesPerView: preview,
        spaceBetween: spacingLg,
        slidesPerGroup: perGroupLg,
      },
    },
  });
}

if ($(".tf-sw-testimonial").length > 0) {
  var preview = $(".tf-sw-testimonial").data("preview");
  var tablet = $(".tf-sw-testimonial").data("tablet");
  var mobile = $(".tf-sw-testimonial").data("mobile");
  var spacingLg = $(".tf-sw-testimonial").data("space-lg");
  var spacingMd = $(".tf-sw-testimonial").data("space-md");
  var spacing = $(".tf-sw-testimonial").data("space");
  var perGroup = $(".tf-sw-testimonial").data("pagination");
  var perGroupMd = $(".tf-sw-testimonial").data("pagination-md");
  var perGroupLg = $(".tf-sw-testimonial").data("pagination-lg");
  var swiper = new Swiper(".tf-sw-testimonial", {
    slidesPerView: mobile,
    spaceBetween: spacing,
    speed: 1000,
    pagination: {
      el: ".sw-pagination-testimonial",
      clickable: true,
    },
    observer: true,
    observeParents: true,
    slidesPerGroup: perGroup,
    navigation: {
      clickable: true,
      nextEl: ".nav-next-testimonial",
      prevEl: ".nav-prev-testimonial",
    },
    breakpoints: {
      768: {
        slidesPerView: tablet,
        spaceBetween: spacingMd,
        slidesPerGroup: perGroupMd,
      },
      1200: {
        slidesPerView: preview,
        spaceBetween: spacingLg,
        slidesPerGroup: perGroupLg,
      },
    },
  });
}

if ($(".tf-sw-shop-gallery").length > 0) {
  var tfSwGallery = $(".tf-sw-shop-gallery");
  var preview = tfSwGallery.data("preview");
  var tablet = tfSwGallery.data("tablet");
  var mobile = tfSwGallery.data("mobile");
  var spacingLg = tfSwGallery.data("space-lg");
  var spacingMd = tfSwGallery.data("space-md");
  var spacing = tfSwGallery.data("space");
  var perGroup = tfSwGallery.data("pagination") || 1;
  var perGroupMd = tfSwGallery.data("pagination-md") || 1;
  var perGroupLg = tfSwGallery.data("pagination-lg") || 1;
  var loop = tfSwGallery.data("loop") !== undefined ?  tfSwGallery.data("loop") : false;
  var centered = tfSwGallery.data("centered") !== undefined ? tfSwGallery.data("centered") : false;
  var mobileSm = tfSwGallery.data("mobile-sm") !== undefined ? tfSwGallery.data("mobile-sm") : mobile;
  var swiper = new Swiper(".tf-sw-shop-gallery", {
    slidesPerView: mobile,
    spaceBetween: spacing,
    observer: true,
    observeParents: true,
    pagination: {
      el: ".sw-pagination-gallery",
      clickable: true,
    },
    loop:loop,
    slidesPerGroup: perGroup,
    centeredSlides: false,
    speed: 1000,
    navigation: {
      clickable: true,
      nextEl: ".nav-next-gallery",
      prevEl: ".nav-prev-gallery",
    },
    breakpoints: {
      575: {
        slidesPerView: mobileSm,
        spaceBetween: spacing,
        slidesPerGroup: perGroup,
      },
      768: {
        slidesPerView: tablet,
        spaceBetween: spacingMd,
        slidesPerGroup: perGroupMd,
        centeredSlides: false,
      },
      1200: {
        slidesPerView: preview,
        spaceBetween: spacingLg,
        slidesPerGroup: perGroupLg,
        centeredSlides: centered,
      },
    },
  });
}

if ($(".tf-sw-iconbox").length > 0) {
  var preview = $(".tf-sw-iconbox").data("preview");
  var tablet = $(".tf-sw-iconbox").data("tablet");
  var mobile = $(".tf-sw-iconbox").data("mobile");
  var mobileSm = $(".tf-sw-iconbox").data("mobile-sm");
  var spacingLg = $(".tf-sw-iconbox").data("space-lg");
  var spacingMd = $(".tf-sw-iconbox").data("space-md");
  var spacing = $(".tf-sw-iconbox").data("space");
  var perGroup = $(".tf-sw-iconbox").data("pagination");
  var perGroupSm = $(".tf-sw-iconbox").data("pagination-sm");
  var perGroupMd = $(".tf-sw-iconbox").data("pagination-md");
  var perGroupLg = $(".tf-sw-iconbox").data("pagination-lg");
  var swiper = new Swiper(".tf-sw-iconbox", {
    slidesPerView: mobile,
    spaceBetween: spacing,
    speed: 1000,
    pagination: {
      el: ".sw-pagination-iconbox",
      clickable: true,
    },
    observer: true,
    observeParents: true,
    slidesPerGroup: perGroup,
    navigation: {
      clickable: true,
      nextEl: ".nav-next-iconbox",
      prevEl: ".nav-prev-iconbox",
    },
    breakpoints: {
      575: {
        slidesPerView: mobileSm,
        spaceBetween: spacingMd,
        slidesPerGroup: perGroupSm,
      },

      768: {
        slidesPerView: tablet,
        spaceBetween: spacingMd,
        slidesPerGroup: perGroupMd,
      },
      1200: {
        slidesPerView: preview,
        spaceBetween: spacingLg,
        slidesPerGroup: perGroupLg,
      },
    },
  });
}

if ($(".tf-sw-lookbook").length > 0) {
  var tfSwLb = $(".tf-sw-lookbook");
  var preview = tfSwLb.data("preview");
  var tablet = tfSwLb.data("tablet");
  var mobile = tfSwLb.data("mobile");
  var spacingLg = tfSwLb.data("space-lg");
  var spacingMd = tfSwLb.data("space-md");
  var spacing = tfSwLb.data("space");
  var perGroup = tfSwLb.data("pagination");
  var perGroupMd = tfSwLb.data("pagination-md");
  var perGroupLg = tfSwLb.data("pagination-lg");
  var mobileSm = tfSwLb.data("mobile-sm") !== undefined ? tfSwLb.data("mobile-sm") : mobile;
  var swiperLb = new Swiper(".tf-sw-lookbook", {
    slidesPerView: mobile,
    spaceBetween: spacing,
    observer: true,
    observeParents: true,
    speed: 1000,
    pagination: {
      el: ".sw-pagination-lookbook",
      clickable: true,
    },
    slidesPerGroup: perGroup,
    navigation: {
      clickable: true,
      nextEl: ".nav-prev-lookbook",
      prevEl: ".nav-next-lookbook",
    },
    breakpoints: {
      575: {
        slidesPerView: mobileSm,
        spaceBetween: spacing,
        slidesPerGroup: perGroup,
      },
      768: {
        slidesPerView: tablet,
        spaceBetween: spacingMd,
        slidesPerGroup: perGroupMd,
      },
      1200: {
        slidesPerView: preview,
        spaceBetween: spacingLg,
        slidesPerGroup: perGroupLg,
      },
    },
  });

  $(".swiper-button").click(function () {
    var slideIndex = $(this).data("slide");
    swiperLb.slideTo(slideIndex,500,false);
  });
}





if ($(".tf-sw-partner").length > 0) {
  var preview = $(".tf-sw-partner").data("preview");
  var tablet = $(".tf-sw-partner").data("tablet");
  var mobile = $(".tf-sw-partner").data("mobile");
  var mobileSm = $(".tf-sw-partner").data("mobile-sm");
  var spacingLg = $(".tf-sw-partner").data("space-lg");
  var spacingMd = $(".tf-sw-partner").data("space-md");
  var spacing = $(".tf-sw-partner").data("space");
  var loop = $(".tf-sw-partner").data("loop");
  var play = $(".tf-sw-partner").data("auto-play");
  var speed = $(".tf-sw-partner").data("speed") !== undefined ? $(".tf-sw-slideshow").data("speed") : 1000;
  var swiper = new Swiper(".tf-sw-partner", {
    autoplay: {
      delay: delay,
      disableOnInteraction: false,
    },
    autoplay: play,
    slidesPerView: mobile,
    spaceBetween: spacing,
    speed: speed,
    loop: loop,
    observer: true,
    observeParents: true,
    pagination: {
      el: ".sw-pagination-partner",
      clickable: true,
    },
    navigation: {
      clickable: true,
      nextEl: ".nav-next-partner",
      prevEl: ".nav-prev-partner",
    },
    breakpoints: {
      575: {
        slidesPerView: mobileSm,
        spaceBetween: spacingMd,
      },

      768: {
        slidesPerView: tablet,
        spaceBetween: spacingMd,
      },
      1200: {
        slidesPerView: preview,
        spaceBetween: spacingLg,
      },
    },
  });
  $(".tf-sw-partner").hover(
    function () {
      this.swiper.autoplay.stop();
    },
    function () {
      this.swiper.autoplay.start();
    }
  );
}


if ($(".tf-sw-mobile").length > 0) {
  var swiperMb;
  var screenWidth = $('.tf-sw-mobile').data('screen');
  function initSwiper() {
    if (matchMedia(`only screen and (max-width: ${screenWidth}px)`).matches) {
      if (!swiperMb) {
        var preview = $(".tf-sw-mobile").data("preview");
        var spacing = $(".tf-sw-mobile").data("space");

        swiperMb = new Swiper(".tf-sw-mobile", {
          slidesPerView: preview,
          spaceBetween: spacing,
          speed: 1000,
          pagination: {
            el: ".sw-pagination-mb",
            clickable: true,
          },
          navigation: {
            clickable: true,
            nextEl: ".nav-prev-mb",
            prevEl: ".nav-next-mb",
          },
        });
      }
    } else {
      if (swiperMb) {
        swiperMb.destroy(true, true); 
        swiperMb = null; 
        $(".tf-sw-mobile .swiper-wrapper").removeAttr('style');
        $(".tf-sw-mobile .swiper-slide").removeAttr('style');
      }
    }
  }

  initSwiper();
  window.addEventListener("resize", function () {
    initSwiper();
  });
}

if ($(".tf-product-header").length > 0) {
  var swiper = new Swiper(".tf-product-header", {
    slidesPerView: 2,
    spaceBetween: 20,
    navigation: {
      clickable: true,
      nextEl: ".nav-prev-product-header",
      prevEl: ".nav-next-product-header",
    },
  });
}