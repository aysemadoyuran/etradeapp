
import PhotoSwipeLightbox from './photoswipe-lightbox.esm.min.js';
import PhotoSwipe from './photoswipe.esm.min.js';

if ($(".thumbs-slider").length > 0) {
    var direction = $(".tf-product-media-thumbs").data("direction");
    var thumbs = new Swiper(".tf-product-media-thumbs", {
      spaceBetween: 10,
      slidesPerView: "auto",
      freeMode: true,
      direction: "vertical",
      watchSlidesProgress: true,
      observer: true,
      observeParents: true,
      breakpoints: {
        0: {
          direction: "horizontal",
        },
        1200: {
          direction: "vertical",
          direction: direction,
        },
      },
      450: {
        direction: "vertical",
      },
    });
    var main = new Swiper(".tf-product-media-main", {
      spaceBetween: 0,
      observer: true,
      observeParents: true,
      navigation: {
        nextEl: ".thumbs-next",
        prevEl: ".thumbs-prev",
      },
      thumbs: {
        swiper: thumbs,
      },
    });

    // color
    function updateActiveColorButton(activeIndex) {
        $(".color-btn").removeClass("active");
    
        var currentSlide = $(".tf-product-media-main .swiper-slide").eq(activeIndex);
        var currentColor = currentSlide.data("color");
        if (currentColor) {
          $(".color-btn[data-color='" + currentColor + "']").addClass("active");
          $('.value-currentColor').text(currentColor);
          $(".select-currentColor").text(currentColor);
        }
    }
    main.on('slideChange', function () {
        updateActiveColorButton(this.activeIndex);
    });

    // Function scroll to the correct slide and thumb
    function scrollToColor(color) {
    var matchingSlides = $(".tf-product-media-main .swiper-slide").filter(function() {
        return $(this).data("color") === color;
    });
    if (matchingSlides.length > 0) {
        var firstIndex = matchingSlides.first().index();
        main.slideTo(firstIndex,1000,false);
        thumbs.slideTo(firstIndex,1000,false);
    }
    }
    $(".color-btn").on("click", function() {
    var color = $(this).data("color");
    
    $(".color-btn").removeClass("active");
    $(this).addClass("active");

    scrollToColor(color);
    });
    updateActiveColorButton(main.activeIndex);

    // material
    function updateActiveOtherVariantBtn(activeIndex) {
        $(".other-variant-btn").removeClass("active");
    
        var currentSlide = $(".tf-product-media-main .swiper-slide").eq(activeIndex);
        var currentOtherVariant = currentSlide.data("other-variant");
        if (currentOtherVariant) {
          $(".other-variant-btn[data-other-variant='" + currentOtherVariant + "']").addClass("active");
          $('.value-currentVariant').text(currentOtherVariant);
          $(".select-currentVariant").text(currentOtherVariant);
        }
    }
    main.on('slideChange', function () {
        updateActiveOtherVariantBtn(this.activeIndex);
    });

    // Function scroll to the correct slide and thumb
    function scrollToOtherVariant(otherVariant) {
    var matchingSlides = $(".tf-product-media-main .swiper-slide").filter(function() {
        return $(this).data("other-variant") === otherVariant;
    });
    if (matchingSlides.length > 0) {
        var firstIndex = matchingSlides.first().index();
        main.slideTo(firstIndex,1000,false);
        thumbs.slideTo(firstIndex,1000,false);
        }
    }
    $(".other-variant-btn").on("click", function() {
    var otherVariant = $(this).data("other-variant");
    
    $(".other-variant-btn").removeClass("active");
    $(this).addClass("active");
    scrollToOtherVariant(otherVariant);
    });
    updateActiveOtherVariantBtn(main.activeIndex);

}

(function ($) {
    "use strict";

    var section_zoom = function () {
        $(".tf-image-zoom").on("mouseover", function () {
            $(this).closest(".section-image-zoom").addClass("zoom-active");
        });
        $(".tf-image-zoom").on("mouseleave", function () {
            $(this).closest(".section-image-zoom").removeClass("zoom-active");
        });
    }

    var image_zoom = function () {
        if (matchMedia("only screen and (min-width: 768px)").matches) {
            var driftAll = document.querySelectorAll('.tf-image-zoom');
            var pane = document.querySelector('.tf-zoom-main');
            $(driftAll).each(function(i, el) {
                new Drift(
                    el, {
                    zoomFactor: 2,
                    paneContainer: pane,
                    inlinePane: false,
                    handleTouch: false,
                    hoverBoundingBox: true,
                    containInline: true,
                    }
                );
            });
        }
    }

    var image_zoom_magnifier = function () {
        var driftAll = document.querySelectorAll('.tf-image-zoom-magnifier');
        $(driftAll).each(function(i, el) {
            new Drift(
                el, {
                zoomFactor: 2,
                inlinePane: true,
                containInline: false,
                }
            );
        });
    }

    var image_zoom_inner = function () {
        var driftAll = document.querySelectorAll('.tf-image-zoom-inner');
        var pane = document.querySelector('.tf-product-zoom-inner');
        $(driftAll).each(function(i, el) {
            new Drift(
                el, {
                paneContainer: pane,
                zoomFactor: 2,
                inlinePane: false,
                containInline: false,
                }
            );
        });
    }

    var lightboxswiper = function () {

        const lightbox = new PhotoSwipeLightbox({
            gallery: '#gallery-swiper-started',
            children: 'a',
            pswpModule: PhotoSwipe,
            bgOpacity: 1,
            secondaryZoomLevel: 2,
            maxZoomLevel: 3,
        });
        lightbox.init();

        lightbox.on('change', () => {
            const { pswp } = lightbox;
            main.slideTo(pswp.currIndex, 0, false);
        });

        lightbox.on('afterInit', () => {
            if (main.params.autoplay.enabled) {
                main.autoplay.stop();
            };
        });

        lightbox.on('closingAnimationStart', () => {
            const { pswp } = lightbox;
            main.slideTo(pswp.currIndex, 0, false);
            if (main.params.autoplay.enabled) {
                main.autoplay.start();
            }
        });

    }
    
    var lightbox = function () {

        const lightbox = new PhotoSwipeLightbox({
            gallery: '#gallery-started',
            children: 'a',
            pswpModule: PhotoSwipe,
            bgOpacity: 1,
            secondaryZoomLevel: 2,
            maxZoomLevel: 3,
        });
        lightbox.init();

    }

    var model_viewer = function () {

        if ($(".tf-model-viewer").length) {
   
            $(".tf-model-viewer-ui-button").on("click", function (e) {
                $(this).closest(".tf-model-viewer").find("model-viewer").removeClass("disabled");
                $(this).closest(".tf-model-viewer").toggleClass("active");
            });
            $(".tf-model-viewer-ui").on("dblclick", function (e) {
                $(this).closest(".tf-model-viewer").find("model-viewer").addClass("disabled");
                $(this).closest(".tf-model-viewer").toggleClass("active");
            });
        }

    }

  // Dom Ready
  $(function () {
    section_zoom();
    image_zoom();
    image_zoom_magnifier();
    image_zoom_inner();
    lightboxswiper();
    lightbox();
    model_viewer();
  });
})(jQuery);


